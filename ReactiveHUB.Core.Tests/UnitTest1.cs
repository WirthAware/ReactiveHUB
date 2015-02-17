using System.Reactive.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class Test
{
    [TestMethod]
    public async Task TestMethod()
    {
        // The api returns a few entries when GetLatestValues is called and then continues with some values in the stream
        var apiMock = new Mock<IRemoteWebAPI>();
        apiMock.Setup(x => x.GetLatestValues()).Returns(Task.FromResult(new[] { 1, 2, 3, 4, 5 }));
        apiMock.Setup(x => x.SubscribeToNewValues()).Returns(new[] { 6, 7, 8, 9, 10 }.ToObservable());

        var myObservable = this.MakeMyObservable(apiMock.Object);

        var items = await myObservable.ToArray().FirstAsync();

        // I want first all the values returned from the async call, followed by everything that the stream returns
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, items);
    }

    public IObservable<int> MakeMyObservable(IRemoteWebAPI myApi)
    {
        throw new NotImplementedException("How to implement this?");
    }
}

public interface IRemoteWebAPI
{
    Task<int[]> GetLatestValues();

    IObservable<int> SubscribeToNewValues();
}