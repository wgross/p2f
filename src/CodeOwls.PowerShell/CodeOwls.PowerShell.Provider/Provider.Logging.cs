/*
	Copyright (c) 2014 Code Owls LLC

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to
	deal in the Software without restriction, including without limitation the
	rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
	sell copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
	IN THE SOFTWARE.
*/

using System;

namespace CodeOwls.PowerShell.Provider
{
    public partial class Provider
    {
        protected void LogDebug(string format, params object[] args)
        {
            try
            {
                WriteDebug(String.Format(format, args));
            }
            catch
            {
            }
        }

        private void ExecuteAndLog(Action action, string methodName, params string[] args)
        {
            var argsList = String.Join(", ", args);
            try
            {
                LogDebug(">> {0}([{1}])", methodName, argsList);
                action();
            }
            catch (Exception e)
            {
                LogDebug("!! {0}([{1}]) EXCEPTION: {2}", methodName, argsList, e.ToString());
                throw;
            }
            finally
            {
                LogDebug("<< {0}([{1}])", methodName, argsList);
            }
        }

        private T ExecuteAndLog<T>(Func<T> action, string methodName, params string[] args)
        {
            var argsList = String.Join(", ", args);
            try
            {
                LogDebug(">> {0}([{1}])", methodName, argsList);
                return action();
            }
            catch (Exception e)
            {
                LogDebug("!! {0}([{1}]) EXCEPTION: {2}", methodName, argsList, e.ToString());
                throw;
            }
            finally
            {
                LogDebug("<< {0}([{1}])", methodName, argsList);
            }
        }
    }
}