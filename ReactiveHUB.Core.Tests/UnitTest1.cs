namespace ReactiveHUB.Core.Tests
{
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class Test
    {
        private Mock<IRemoteWebAPI> remoteWebAPI;

        [TestInitialize]
        public void testInit()
        {
            // The api returns a few entries when GetLatestValues is called and then continues with some values in the stream
            remoteWebAPI = new Mock<IRemoteWebAPI>();
            remoteWebAPI.Setup(x => x.SubscribeToNewValues()).Returns(new[] { 6, 7, 8, 9, 10 }.ToObservable());
            remoteWebAPI.Setup(x => x.GetLatestValues()).Returns(this.GetInts);
        }

        [TestMethod]
        public async Task TestMethod()
        {

            var myObservable = await this.MakeMyObservable(remoteWebAPI.Object);

            var items = await myObservable.ToArray().FirstAsync();

            // I want first all the values returned from the async call, followed by everything that the stream returns
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, items);
        }

        [TestMethod]
        public void WhenOnCompletedIsCalledSubscriptionsAreDisposedByReactiveExtensions()
        {
            var called = false;
            var observable = Observable.Create<Unit>(
                observer =>
                    {
                        observer.OnCompleted();
                        return Disposable.Create(() => called = true);
                    });

            observable.Subscribe(_ => { }, () => { });

            Assert.IsTrue(called);
        }

        public async Task<IObservable<int>> MakeMyObservable(IRemoteWebAPI myApi)
        {
            var latest = await myApi.GetLatestValues();
            var newer = myApi.SubscribeToNewValues();
            return latest.ToObservable().Concat(newer);
        }

        public async Task<int[]> GetInts()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new[] { 1, 2, 3, 4, 5 };
        }
    }

    public interface IRemoteWebAPI
    {
        Task<int[]> GetLatestValues();

        IObservable<int> SubscribeToNewValues();
    }
}