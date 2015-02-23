namespace ProjectTemplate.WebRequests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

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

                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                r.Headers[header.Key] = header.Value;
                            }
                        }
                    });
        }

        public IObservable<Unit> Send(WebRequestData data, IScheduler sched = null)
        {
            return this.SendAndReceive<Unit, Unit>(data, DoNotReceive, null, sched);
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
                        var res = new CompositeDisposable(new IDisposable[] { t });
                        IWebRequest req = null;

                        // Steps 6+: Run the receiveStep function in a loop until the subscription is disposed
                        Action<TReceiveOn> doReceiveStep = null;
                        doReceiveStep = receiveOn =>
                            {
                                receiveStep(receiveOn, observer);
                                t.Disposable = sched.Schedule(() => doReceiveStep(receiveOn));
                            };

                        // Step 5: Apply the given transformation on the stream and skip steps 6+ if no receive step function is given
                        Action<IWebResponse> prepareReceive = r =>
                            {
                                var receiveOn = transformation(r, observer, res);
                                if (receiveStep != null)
                                {
                                    t.Disposable = sched.Schedule(() => doReceiveStep(receiveOn));
                                }
                                else
                                {
                                    observer.OnCompleted();
                                }
                            };

                        // Step 4: Get the response and the response stream
                        Action getResponseStream = () => req.GetResponse().Then(
                            r =>
                                {
                                    res.Add(r);
                                    t.Disposable = sched.Schedule(() => prepareReceive(r));
                                    return Unit.Default;
                                });

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
                        Action getRequestStream = () => req.GetRequestStream().Then(
                            s =>
                                {
                                    // The stream needs to be disposed when the scheduled SendData is never executed
                                    res.Add(s);
                                    t.Disposable = sched.Schedule(() => sendData(s));
                                    return Unit.Default;
                                });

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

                        t.Disposable = sched.Schedule(createRequest);

                        return res;
                    });
        }
    }
}