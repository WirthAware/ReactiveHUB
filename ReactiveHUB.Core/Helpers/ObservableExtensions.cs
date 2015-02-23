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
        /// <param name="self">The Task to apply the FlatMap to</param>
        /// <param name="modifier">The function to apply to the tasks result</param>
        /// <typeparam name="TIn">The type of the result the task this function is invoked on returns.</typeparam>
        /// <typeparam name="TOut">The type of the result the task this function creates returns</typeparam>
        /// <remarks>
        /// This method applies the modifier function to the result of the task when the task is successfully. 
        /// Otherwise it the returned task will fail with the same exception that the original task failed with.
        /// </remarks>
        /// <returns>A task which returns the result of the input task modified by the function</returns>
        public static async Task<TOut> Then<TIn, TOut>(this Task<TIn> self, Func<TIn, TOut> modifier)
        {
            return modifier(await self);
        }

        /// <summary>
        /// FlatMap for Tasks.
        /// Makes Task into a monad (as used by Erik Meijer in the Coursera videos about Reactive Programming)
        /// </summary>
        /// <param name="self">The Task to apply the FlatMap to</param>
        /// <param name="modifier">The function to apply to the tasks result</param>
        /// <typeparam name="TIn">The type of the result the task this function is invoked on returns.</typeparam>
        /// <typeparam name="TOut">The type of the result the task this function creates returns</typeparam>
        /// <remarks>
        /// This method applies the modifier function to the result of the task when the task is successfully. 
        /// Otherwise it the returned task will fail with the same exception that the original task failed with.
        /// </remarks>
        /// <returns>A task which returns the result of the input task modified by the function</returns>
        /// TODO: Add result unpacking behavior description to comment
        public static async Task<TOut> Then<TIn, TOut>(this Task<TIn> self, Func<TIn, Task<TOut>> modifier)
        {
            return await modifier(await self);
        }
    }
}