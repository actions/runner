// Copyright 2012 Netflix, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Threading;

namespace GitHub.Services.CircuitBreaker
{
    public interface IRollingNumber
    {
        /// <summary>
        /// Gets the total window size in milliseconds.
        /// </summary>
        int GetTotalWindowTimeInMilliseconds();

        /// <summary>
        /// Gets the number of parts to break the time window.
        /// </summary>
        int GetNumberOfBuckets();

        /// <summary>
        /// Gets the bucket size in milliseconds. (TimeInMilliseconds / NumberOfBuckets)
        /// </summary>
        int GetBucketSizeInMilliseconds();

        void Reset();

        /// <summary>
        /// Get the sum of all buckets in the rolling counter.
        /// </summary>
        /// <returns>Value for the given <see cref="RollingNumber"/>.</returns>
        long GetRollingSum();

        /// <summary>
        /// Increment the counter in the current bucket by one.
        /// </summary>
        void Increment();
    }

    public class RollingNumber : IRollingNumber
    {
        internal struct Bucket
        {
            internal long windowStart;
            internal long count;
        }

        /// <summary>
        /// The object used to synchronize the <see cref="GetCurrentBucketIndex"/> method.
        /// </summary>
        private readonly object newBucketLock = new object();

        /// <summary>
        /// The <see cref="ITime"/> instance to measure time.
        /// </summary>
        private readonly ITime time;

        /// <summary>
        /// The size of the time window to track per bucket.
        /// </summary>
        private readonly int bucketSizeInMilliseconds;

        /// <summary>
        /// The number of parts to break the time window.
        /// </summary>
        private readonly int numberOfBuckets;

        /// <summary>
        /// An array of numberOfBuckets buckets used to maintain a rolling sum over timeInMilliseconds seconds.
        /// </summary>
        Bucket[] buckets;

        /// <summary>
        /// An index to the current bucket
        /// </summary>
        private int CurrentBucketIndex = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollingNumber"/> class.
        /// </summary>
        /// <param name="timeInMilliseconds">The total time window to track.</param>
        /// <param name="numberOfBuckets">The number of parts to break the time window.</param>
        internal RollingNumber(ITime time, int timeInMilliseconds, int numberOfBuckets)
        {
            if (timeInMilliseconds % numberOfBuckets != 0)
            {
                throw new ArgumentException("The timeInMilliseconds must divide equally into numberOfBuckets. For example 1000/10 is ok, 1000/11 is not.");
            }

            this.time = time;
            this.bucketSizeInMilliseconds = timeInMilliseconds / numberOfBuckets;
            this.numberOfBuckets = numberOfBuckets;
            buckets = new Bucket[numberOfBuckets];
        }

        /// <summary>
        /// Gets the total window size in milliseconds.
        /// </summary>
        public int GetTotalWindowTimeInMilliseconds()
        {
            return this.numberOfBuckets * this.bucketSizeInMilliseconds;
        }

        /// <summary>
        /// Gets the number of parts to break the time window.
        /// </summary>
        public int GetNumberOfBuckets()
        {
            return this.numberOfBuckets;
        }

        /// <summary>
        /// Gets the bucket size in milliseconds. (TimeInMilliseconds / NumberOfBuckets)
        /// </summary>
        public int GetBucketSizeInMilliseconds()
        {
            return this.bucketSizeInMilliseconds;
        }

        /// <summary>
        /// Gets the internal array to store the buckets. This property is intended to use only in unit tests.
        /// </summary>
        internal Bucket[] Buckets
        {
            get { return this.buckets; }
        }

        /// <summary>
        /// Force a reset of all rolling counters (clear all buckets) so that statistics start being gathered from scratch.
        /// This does NOT reset the CumulativeSum values.
        /// </summary>
        public void Reset()
        {
            // clear buckets so we start over again
            long bucketTime = time.GetCurrentTimeInMillis();
            for (int x = 0; x < numberOfBuckets; x++)
            {
                buckets[x].windowStart = bucketTime;
                buckets[x].count = 0;
                bucketTime += bucketSizeInMilliseconds;
            }
        }

        /// <summary>
        /// Get the sum of all buckets in the rolling counter.
        /// </summary>
        /// <returns>Value for the given <see cref="RollingNumber"/>.</returns>
        public long GetRollingSum()
        {
            // ensure that if the time passed is greater than the entire rolling counter we clear it all and start from scratch
            GetCurrentBucketIndex();

            long sum = 0;
            for (int x = 0; x < numberOfBuckets; x++)
            {
                sum += buckets[x].count;
            }

            return sum;
        }

        /// <summary>
        /// Increment the counter in the current bucket by one.
        /// </summary>
        public void Increment()
        {
            Interlocked.Increment(ref buckets[GetCurrentBucketIndex()].count);
        }

        /// <summary>
        /// Gets the current bucket. If the time is after the window of the current bucket, a new one will be initialized.
        /// Internal because it's used in unit tests.
        /// </summary>
        /// <returns>The current bucket.</returns>
        internal int GetCurrentBucketIndex()
        {
            long currentTime = time.GetCurrentTimeInMillis();

            // A shortcut to try and get the most common result of immediately finding the current bucket.
            // Retrieve the current bucket if the given time is BEFORE the end of the bucket window.
            if (currentTime < buckets[CurrentBucketIndex].windowStart + bucketSizeInMilliseconds)
            {
                // If we're within the bucket 'window of time' return the current one
                // NOTE: We do not worry if we are BEFORE the window in a weird case of where thread scheduling causes that to occur,
                // we'll just use the latest as long as we're not AFTER the window
                return CurrentBucketIndex;
            }

            // If we didn't find the current bucket above, then we have to find it.
            //
            // The following needs to be synchronized/locked because the logic involves multiple steps. I am using a TryEnter if/then 
            // so that a single thread will get the lock and as soon as one thread gets the lock all others will go the 'else' block
            // and just return the currentBucket until the newBucket is initialized. This should allow the throughput to be far higher
            // and only slow down 1 thread instead of blocking all of them in each cycle of initializing a new bucket.
            // 
            // This means the timing won't be exact to the millisecond as to what data ends up in a bucket, but that's acceptable.
            // It's not critical to have exact precision to the millisecond, as long as it's rolling, if we can instead reduce the 
            // impact of synchronization.
            // 
            // This is an example of favoring write-performance instead of read-performance.
            if (Monitor.TryEnter(newBucketLock))
            {
                try
                {
                    long bucketTime;

                    // If the time passed is greater than the entire rolling counter so we want to clear it all and start from scratch
                    if (currentTime - (buckets[CurrentBucketIndex].windowStart + (bucketSizeInMilliseconds * numberOfBuckets)) > (bucketSizeInMilliseconds * numberOfBuckets))
                    {
                        bucketTime = currentTime;

                        // Initialize the buckets
                        for (int x = 0; x < numberOfBuckets; x++)
                        {
                            buckets[x].windowStart = bucketTime;
                            buckets[x].count = 0;
                            bucketTime += bucketSizeInMilliseconds;
                        }

                        // be careful here, make sure current bucket is initialized before changing the CurrentBucketIndex index as another thread
                        // could access it in the TryEnter/else statement below
                        CurrentBucketIndex = 0;
                        return CurrentBucketIndex;
                    }

                    // calculate the next bucket
                    int increment = (int)((currentTime - buckets[CurrentBucketIndex].windowStart) / bucketSizeInMilliseconds);

                    // We go into a loop so that it will initialize as many buckets as needed to catch up to the current time
                    // as we want the buckets complete even if we don't have transactions during a period of time.
                    bucketTime = buckets[CurrentBucketIndex].windowStart;
                    int index = CurrentBucketIndex;
                    for (int x = 0; x < increment; x++)
                    {
                        bucketTime += bucketSizeInMilliseconds;
                        index = ++index % numberOfBuckets;
                        buckets[index].windowStart = bucketTime;
                        buckets[index].count = 0;
                    }

                    // be careful here, make sure current bucket is initialized before changing the CurrentBucketIndex index as another thread
                    // could access it in the TryEnter/else statement below
                    CurrentBucketIndex = index;
                    return CurrentBucketIndex;
                }
                finally
                {
                    Monitor.Exit(this.newBucketLock);
                }
            }
            else
            {
                return CurrentBucketIndex;
            }
        }
    }

    /// <summary>
    /// Counter that never counts
    /// </summary>
    public class RollingNumberNoOpImpl : IRollingNumber
    {
        /// <summary>
        /// Gets the total window size in milliseconds.
        /// </summary>
        public int GetTotalWindowTimeInMilliseconds() { return 0; }

        /// <summary>
        /// Gets the number of parts to break the time window.
        /// </summary>
        public int GetNumberOfBuckets() { return 0; }

        /// <summary>
        /// Gets the bucket size in milliseconds. (TimeInMilliseconds / NumberOfBuckets)
        /// </summary>
        public int GetBucketSizeInMilliseconds() { return 0; }

        public void Reset() { }

        /// <summary>
        /// Get the sum of all buckets in the rolling counter.
        /// </summary>
        /// <returns>Value for the given <see cref="RollingNumber"/>.</returns>
        public long GetRollingSum() { return 0; }

        /// <summary>
        /// Increment the counter in the current bucket by one.
        /// </summary>
        public void Increment() { }
    }
}
