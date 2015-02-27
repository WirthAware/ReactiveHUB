namespace ProjectTemplate.WebRequests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Text;

    using ProjectTemplate.Helpers;

    /// <summary>
    /// A service for creating and making web requests. 
    /// It should bridge the gap between the <see cref="System.Net.WebRequest"/> and reactive programming
    /// </summary>
    /// <remarks>
    /// Method names are subject to change, so the fluent syntax looks sensible.
    /// Naming suggestions are appreciated.
    /// </remarks>
    public class WebRequestService : IWebRequestService
    {
        private readonly IScheduler scheduler;

        private readonly Func<Uri, IWebRequest> webRequestFactory;

        public WebRequestService(IScheduler scheduler = null, Func<Uri, IWebRequest> factory = null)
        {
            this.scheduler = scheduler ?? Scheduler.Default;
            this.webRequestFactory = factory ?? (uri => new WebRequest(System.Net.WebRequest.Create(uri)));
        }

        public static void ApplyHeaders(Dictionary<string, string> headers, IWebRequest r)
        {
            if (headers == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                r.Headers[header.Key] = header.Value;
            }
        }

        public WebRequestData Create(Uri uri, Action<IWebRequest> modifierAction = null, byte[] data = null)
        {
            Func<IWebRequest> requestFactory = () =>
                {
                    var req = this.webRequestFactory(uri);

                    if (modifierAction != null)
                    {
                        modifierAction(req);
                    }

                    return req;
                };

            return new WebRequestData(requestFactory, data, this);
        }

        public WebRequestData CreateGet(Uri uri, Dictionary<string, string> headers = null)
        {
            return this.Create(
                uri,
                r =>
                {
                    r.Method = "GET";

                    ApplyHeaders(headers, r);
                });
        }

        public WebRequestData CreatePost(Uri uri, string data, Dictionary<string, string> headers = null, Encoding encoding = null)
        {
            return this.Create(
                uri,
                r =>
                    {
                        r.Method = "POST";
                    ApplyHeaders(headers, r);
                }, 
                (encoding ?? Encoding.UTF8).GetBytes(data));
        }

        public IObservable<Unit> Send(WebRequestData data, IScheduler sched = null)
        {
            return this.SendAndReceive<Unit, Unit>(data, DoNotReceive, null, sched);
        }

        public IObservable<byte> SendAndReadBytewise(WebRequestData data, bool stopAtEndOfStream = false, IScheduler sched = null)
        {
            return this.SendAndReceive<byte, Stream>(data, FetchResponseStream, (stream, observer) => ReadByte(stream, observer, stopAtEndOfStream), sched);
        }

        public IObservable<string> SendAndReadLinewise(WebRequestData data, Encoding encoding = null, bool stopAtEndOfStream = false, IScheduler sched = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            return this.SendAndReceive<string, StreamReader>(
                data,
                (response, observer, d) => FetchResponseReader(response, observer, d, encoding),
                (reader, observer) =>
                {
                    if (reader.EndOfStream && stopAtEndOfStream)
                    {
                        observer.OnCompleted();
                    }
                    else
                    {
                        observer.OnNext(reader.ReadLine());
                    }
                }, 
                sched);
        }

        public IObservable<string> SendAndReadAllText(
            WebRequestData data,
            Encoding encoding = null,
            IScheduler sched = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            return this.SendAndReceive<string, StreamReader>(
                data,
                (response, observer, d) => FetchResponseReader(response, observer, d, encoding),
                (reader, observer) =>
                {
                    observer.OnNext(reader.ReadToEnd());
                    observer.OnCompleted();
                },
                sched);
        }

        private static void ReadByte(Stream stream, IObserver<byte> observer, bool stopAtEndOfStream)
        {
            var result = stream.ReadByte();
            if (result != -1)
            {
                observer.OnNext((byte)result);
            }
            else
            {
                if (stopAtEndOfStream)
                {
                    observer.OnCompleted();
                }
            }
        }

        // ReSharper disable once UnusedParameter.Local Justification: This signature is mandatory for the usage
        private static StreamReader FetchResponseReader<T>(IWebResponse response, IObserver<T> observer, ICollection<IDisposable> d, Encoding encoding)
        {
            var result = new StreamReader(response.GetResponseStream(), encoding);

            // Dispose the reader when done
            d.Add(result);

            return result;
        }

        private static Stream FetchResponseStream(IWebResponse response, IObserver<byte> observer, CompositeDisposable d)
        {
            var result = response.GetResponseStream();

            // Dispose the stream when done
            d.Add(result);

            return result;
        }

        private static Unit DoNotReceive(IWebResponse webResponse, IObserver<Unit> observer, CompositeDisposable d)
        {
            observer.OnNext(Unit.Default);
            return Unit.Default;
        }

        private IObservable<T> SendAndReceive<T, TReceiveOn>(
            WebRequestData data,
            Func<IWebResponse, IObserver<T>, CompositeDisposable, TReceiveOn> transformation,
            Action<TReceiveOn, IObserver<T>> receiveStep,
            IScheduler sched = null)
        {
            if (sched == null)
            {
                sched = this.scheduler;
            }

            return Observable.Create<T>(
                observer =>
                {
                    var t = new MultipleAssignmentDisposable();
                    var b = new BooleanDisposable();
                    var res = new CompositeDisposable(new IDisposable[] { t, b });
                    IWebRequest req = null;

                    // This additional check is done for when the ImmediateScheduler is used.
                    Action<Action> schedule = a =>
                    {
                        if (!b.IsDisposed)
                        {
                            t.Disposable = sched.Schedule(a);
                        }
                    };

                    // Steps 6+: Run the receiveStep function in a loop until the subscription is disposed
                    Action<TReceiveOn> doReceiveStep = null;
                    doReceiveStep = receiveOn =>
                        {
                            receiveStep(receiveOn, observer);
                            schedule(() => doReceiveStep(receiveOn));
                        };

                    // Step 5: Apply the given transformation on the stream and skip steps 6+ if no receive step function is given
                    Action<IWebResponse> prepareReceive = r =>
                        {
                            var receiveOn = transformation(r, observer, res);
                            if (receiveStep != null)
                            {
                                // TODO: Check if IScheduler.Schedule(Action<Action>>) is better here
                                schedule(() => doReceiveStep(receiveOn));
                            }
                            else
                            {
                                observer.OnCompleted();
                            }
                        };

                    // Step 4: Get the response and the response stream
                    // ReSharper disable once PossibleNullReferenceException
                    Action getResponseStream = () => req.GetResponse().Subscribe(
                        r =>
                        {
                            res.Add(r);
                            schedule(() => prepareReceive(r));
                        }, 
                        observer.OnError);

                    // Step 3: Send the data and get the response
                    Action<Stream> sendData = s =>
                        {
                            // Don't dispose the stream while sending data!
                            using (s)
                            {
                                s.Write(data.DataToSend, 0, data.DataToSend.Length);
                            }

                            getResponseStream();
                        };

                    // Step 2: Get the request stream and schedule sending the data
                    // ReSharper disable once PossibleNullReferenceException
                    Action getRequestStream = () => req.GetRequestStream().Subscribe(
                        s =>
                        {
                            // The stream needs to be disposed when the scheduled SendData is never executed
                            res.Add(s);
                            schedule(() => sendData(s));
                        }, 
                        observer.OnError);

                    // Step 1: Create the request and decide whether to send data or get the response immediately (= Skip steps 2 & 3)
                    Action createRequest = () =>
                        {
                            req = data.RequestFactory();

                            if (data.DataToSend != null)
                            {
                                getRequestStream();
                            }
                            else
                            {
                                getResponseStream();
                            }
                        };

                    schedule(createRequest);

                    return res;
                });
        }
    }
}