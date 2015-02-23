namespace ProjectTemplate.Helpers
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods to <see cref="IObservable{T}"/> and <see cref="IObserver{T}"/>
    /// </summary>
    public static class ObservableExtensions
    {
        /// <summary>
        /// FlatMap for Tasks.
        /// Makes Task into a monad (as used by Erik Meijer in the Coursera videos about Reactive Programming)
        /// </summary>
        /// <remarks>
        /// This method applies the modifier function to the result of the task when the task is successfully. 
        /// Otherwise it the returned task will fail with the same exception that the original task failed with.
        /// </remarks>
        public static async Task<TOut> Then<TIn, TOut>(this Task<TIn> self, Func<TIn, TOut> modifier)
        {
            return modifier(await self);
        }

        /// <summary>
        /// FlatMap for Tasks.
        /// Makes Task into a monad (as used by Erik Meijer in the Coursera videos about Reactive Programming)
        /// </summary>
        /// <remarks>
        /// This method applies the modifier function to the result of the task when the task is successfully. 
        /// Otherwise it the returned task will fail with the same exception that the original task failed with.
        /// </remarks>
        /// TODO: Add result unpacking behavior description to comment
        public static async Task<TOut> Then<TIn, TOut>(this Task<TIn> self, Func<TIn, Task<TOut>> modifier)
        {
            return await modifier(await self);
        }
    }
}