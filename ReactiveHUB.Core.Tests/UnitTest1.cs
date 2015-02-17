using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        remoteWebAPI.Setup(x => x.GetLatestValues()).Returns(Task.FromResult(new[] { 1, 2, 3, 4, 5 }));
        remoteWebAPI.Setup(x => x.SubscribeToNewValues()).Returns(new[] { 6, 7, 8, 9, 10 }.ToObservable());
    }  

    [TestMethod]
    public async Task TestMethod()
    {

        var myObservable = this.MakeMyObservable(remoteWebAPI.Object);

        var items = await myObservable.ToArray().FirstAsync();

        // I want first all the values returned from the async call, followed by everything that the stream returns
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, items);
    }

    public IObservable<int> MakeMyObservable(IRemoteWebAPI myApi)
    {
        IObservable<int> result = new int[] {}.ToObservable();
        var latest = myApi.GetLatestValues().ToObservable();
        var newer = myApi.SubscribeToNewValues();

        var hotSource = Observable.Publish(latest);
        
        hotSource.Subscribe(ints =>
        {
            var tmp = ints.ToObservable();
            result = tmp.Concat(newer);
        });
        hotSource.Connect();

        return result;
    }
}

public interface IRemoteWebAPI
{
    Task<int[]> GetLatestValues();

    IObservable<int> SubscribeToNewValues();
}