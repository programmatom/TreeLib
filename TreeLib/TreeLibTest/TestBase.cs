/*
 *  Copyright © 2016 Thomas R. Lawrence
 * 
 *  GNU Lesser General Public License
 * 
 *  This file is part of TreeLib
 * 
 *  TreeLib is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program. If not, see <http://www.gnu.org/licenses/>.
 * 
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class TestBase
    {
        public TestBase()
        {
        }

        public TestBase(long[] breakIterations, long startIteration)
        {
            this.iteration = startIteration;
            this.breakIterations = breakIterations;
        }

        public long iteration { get; private set; }
        private readonly long[] breakIterations = new long[0];

        protected long lastActionIteration;

        protected void IncrementIteration()
        {
            iteration = unchecked(iteration + 1);
            if (Array.IndexOf(breakIterations, iteration) >= 0)
            {
                Debug.Assert(false, String.Format("BREAK AT ITERATION {0}", iteration));
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }
        }

        public delegate void VoidAction();
        public delegate bool BoolAction();

        public void TestNoThrow(string label, VoidAction action)
        {
            IncrementIteration();
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Console.WriteLine("LAST ITERATION {0}, LAST ACTION ITERATION {1}", iteration, lastActionIteration);
                Console.WriteLine("Unexpected exception occurred: {0}", exception);
                throw new UnitTestFailureException(label, exception);
            }
        }

        public void TestThrow(string label, Type exceptionType, VoidAction action)
        {
            IncrementIteration();
            try
            {
                action();
                Console.WriteLine("Expected exception did not occur");
                throw new UnitTestFailureException(label, new Exception("Expected exception did not occur"));
            }
            catch (Exception exception) when (exceptionType.IsAssignableFrom(exception.GetType()))
            {
            }
            catch (Exception exception)
            {
                Console.WriteLine("LAST ITERATION {0}, LAST ACTION ITERATION {1}", iteration, lastActionIteration);
                Console.WriteLine("Unexpected exception occurred: {0}", exception);
                throw new UnitTestFailureException(label, exception);
            }
        }

        public void TestBool(string label, bool value, BoolAction action)
        {
            IncrementIteration();
            try
            {
                if (value != action())
                {
                    throw new UnitTestFailureException(label);
                }
            }
            catch (Exception exception) when (!(exception is UnitTestFailureException))
            {
                Console.WriteLine("LAST ITERATION {0}, LAST ACTION ITERATION {1}", iteration, lastActionIteration);
                Console.WriteLine("Unexpected exception occurred: {0}", exception);
                throw new UnitTestFailureException(label, exception);
            }
        }

        public void TestTrue(string label, BoolAction action)
        {
            TestBool(label, true, action);
        }

        public void TestFalse(string label, BoolAction action)
        {
            TestBool(label, false, action);
        }

        public void Fault(object faultingObject, string description, Exception innerException)
        {
            Console.WriteLine("LAST ITERATION {0}, LAST ACTION ITERATION {1}", iteration, lastActionIteration);
            string message = String.Format("{0}: {1}", faultingObject != null ? faultingObject.GetType().Name : "<null>", description);
            if (innerException != null)
            {
                message = String.Concat(Environment.NewLine, "Initial exception: ", innerException);
            }
            Console.WriteLine(message);
            bool throwError = true;
            Debug.Assert(false, description);
            Debugger.Break();
            if (throwError)
            {
                throw new UnitTestFailureException(message);
            }
        }

        public void Fault(object faultingObject, string description)
        {
            Fault(faultingObject, description, null);
        }


        private ConsoleBuffer consoleBuffer;

        public ConsoleBuffer ConsoleBuffer { set { consoleBuffer = value; } }

        public void WriteLine(string line)
        {
            if (consoleBuffer != null)
            {
                consoleBuffer.WriteText(line);
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        public void WriteLine(string format, params object[] args)
        {
            string line = String.Format(format, args);
            if (consoleBuffer != null)
            {
                consoleBuffer.WriteText(line);
            }
            else
            {
                Console.WriteLine(line);
            }
        }


        protected void ShowException(string testName, Exception exception)
        {
            WriteLine("LAST ITERATION {0}, LAST ACTION ITERATION {1}", iteration, lastActionIteration);
            WriteLine("{0} Failure: {1}", testName, exception.Message);
            if (exception.InnerException != null)
            {
                WriteLine("  inner: {0}", exception.InnerException.Message);
            }
            Debug.Assert(false, exception.ToString());
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }


        public virtual bool Do()
        {
            throw new NotSupportedException();
        }

        public virtual bool Do(int seed, StochasticControls control)
        {
            throw new NotSupportedException();
        }


        protected delegate void InvokeAction<TreeType>(TreeType[] collections, Random rnd, ref string lastActionDescription);
        protected delegate void ValidateMethod<TreeType>(TreeType[] collections);
        protected bool StochasticDriver<TreeType>(
            string title,
            int seed,
            StochasticControls control,
            TreeType[] collections,
            Tuple<Tuple<int, int>, InvokeAction<TreeType>>[] actions,
            ValidateMethod<TreeType> validate)
        {
            try
            {
                Random rnd = new Random(seed);

                int totalProb1 = 0;
                int totalProb2 = 0;
                for (int i = 0; i < actions.Length; i++)
                {
                    totalProb1 += actions[i].Item1.Item1;
                    totalProb2 += actions[i].Item1.Item2;
                }
                Debug.Assert((totalProb1 > 0) && (totalProb2 > 0));

                const int RegimeDuration = 50000;
                int regime = 0;
                long iterations = 0;
                uint maxCountEver = 0;
                uint maxCount1 = 0;
                uint minCount1 = 0;
                while (!control.Stop)
                {
                    iterations++;
                    uint lastCount = ((INonInvasiveTreeInspection)collections[0]).Count;
                    maxCountEver = Math.Max(maxCountEver, lastCount);
                    maxCount1 = Math.Max(maxCount1, lastCount);
                    minCount1 = Math.Min(minCount1, lastCount);
                    if (iterations % control.ReportingInterval == 0)
                    {
                        WriteLine("  iterations: {0:N0}  r {1}  minc {2}  maxc {3}  lastc {4}  maxc* {5}", iterations, regime, minCount1, maxCount1, lastCount, maxCountEver);
                        minCount1 = lastCount;
                        maxCount1 = lastCount;
                    }
                    if (iterations % RegimeDuration == 0)
                    {
                        regime = regime ^ 1;
                    }

                    string lastActionDescription = String.Empty;

                    int selector = rnd.Next(regime == 0 ? totalProb1 : totalProb2);
                    for (int i = 0; i < actions.Length; i++)
                    {
                        int selector1 = selector;
                        selector -= regime == 0 ? actions[i].Item1.Item1 : actions[i].Item1.Item2;
                        if (selector1 < (regime == 0 ? actions[i].Item1.Item1 : actions[i].Item1.Item2))
                        {
                            lastActionIteration = iteration + 1; // save iteration for setting breaks on rerun
                            IncrementIteration(); // allow breaks at predictable location
                            actions[i].Item2(collections, rnd, ref lastActionDescription);
                            break;
                        }
                    }
                    Debug.Assert(selector < 0);

                    validate(collections);
                }
            }
            catch (Exception exception)
            {
                control.Failed = true;
                ShowException(title, exception);
                return false;
            }

            return true;
        }
    }
}
