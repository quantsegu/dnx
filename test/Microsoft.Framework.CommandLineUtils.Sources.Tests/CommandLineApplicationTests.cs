// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Framework.Runtime.Common.CommandLine
{
    public class CommandLineApplicationTests
    {
        [Fact]
        public void CommandNameCanBeMatched()
        {
            var called = false;

            var app = new CommandLineApplication();
            app.Command("test", c =>
            {
                c.OnExecute(() =>
                {
                    called = true;
                    return 5;
                });
            });

            var result = app.Execute("test");
            Assert.Equal(5, result);
            Assert.True(called);
        }

        [Fact]
        public void RemainingArgsArePassed()
        {
            CommandArgument first = null;
            CommandArgument second = null;

            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                first = c.Argument("first", "First argument");
                second = c.Argument("second", "Second argument");
                c.OnExecute(() => 0);
            });

            app.Execute("test", "one", "two");

            Assert.Equal("one", first.Value);
            Assert.Equal("two", second.Value);
        }

        [Fact]
        public void ExtraArgumentCausesException()
        {
            CommandArgument first = null;
            CommandArgument second = null;

            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                first = c.Argument("first", "First argument");
                second = c.Argument("second", "Second argument");
                c.OnExecute(() => 0);
            });

            var ex = Assert.Throws<Exception>(() => app.Execute("test", "one", "two", "three"));

            Assert.Contains("three", ex.Message);
        }

        [Fact]
        public void UnknownCommandCausesException()
        {
            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                c.Argument("first", "First argument");
                c.Argument("second", "Second argument");
                c.OnExecute(() => 0);
            });

            var ex = Assert.Throws<Exception>(() => app.Execute("test2", "one", "two", "three"));

            Assert.Contains("test2", ex.Message);
        }

        [Fact]
        public void MultipleValuesArgumentConsumesAllArgumentValues()
        {
            CommandArgument argument = null;

            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                argument = c.Argument("arg", "Argument that allows multiple values", multipleValues: true);
                c.OnExecute(() => 0);
            });

            app.Execute("test", "one", "two", "three", "four", "five");

            Assert.Equal(new[] { "one", "two", "three", "four", "five" }, argument.Values);
        }

        [Fact]
        public void MultipleValuesArgumentConsumesAllRemainingArgumentValues()
        {
            CommandArgument first = null;
            CommandArgument second = null;
            CommandArgument third = null;

            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                first = c.Argument("first", "First argument");
                second = c.Argument("second", "Second argument");
                third = c.Argument("third", "Third argument that allows multiple values", multipleValues: true);
                c.OnExecute(() => 0);
            });

            app.Execute("test", "one", "two", "three", "four", "five");

            Assert.Equal("one", first.Value);
            Assert.Equal("two", second.Value);
            Assert.Equal(new[] { "three", "four", "five" }, third.Values);
        }

        [Fact]
        public void MultipleValuesArgumentMustBeTheLastArgument()
        {
            var app = new CommandLineApplication();
            app.Argument("first", "First argument", multipleValues: true);
            var ex = Assert.Throws<InvalidOperationException>(() => app.Argument("second", "Second argument"));

            Assert.Contains($"The last argument 'first' accepts multiple values. No more argument can be added.",
                ex.Message);
        }

        [Fact]
        public void OptionSwitchMayBeProvided()
        {
            CommandOption first = null;
            CommandOption second = null;

            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                first = c.Option("--first <NAME>", "First argument", CommandOptionType.SingleValue);
                second = c.Option("--second <NAME>", "Second argument", CommandOptionType.SingleValue);
                c.OnExecute(() => 0);
            });

            app.Execute("test", "--first", "one", "--second", "two");

            Assert.Equal("one", first.Values[0]);
            Assert.Equal("two", second.Values[0]);
        }

        [Fact]
        public void OptionValueMustBeProvided()
        {
            CommandOption first = null;

            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                first = c.Option("--first <NAME>", "First argument", CommandOptionType.SingleValue);
                c.OnExecute(() => 0);
            });

            var ex = Assert.Throws<Exception>(() => app.Execute("test", "--first"));

            Assert.Contains("missing value for option", ex.Message);
        }

        [Fact]
        public void ValuesMayBeAttachedToSwitch()
        {
            CommandOption first = null;
            CommandOption second = null;

            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                first = c.Option("--first <NAME>", "First argument", CommandOptionType.SingleValue);
                second = c.Option("--second <NAME>", "Second argument", CommandOptionType.SingleValue);
                c.OnExecute(() => 0);
            });

            app.Execute("test", "--first=one", "--second:two");

            Assert.Equal("one", first.Values[0]);
            Assert.Equal("two", second.Values[0]);
        }

        [Fact]
        public void ShortNamesMayBeDefined()
        {
            CommandOption first = null;
            CommandOption second = null;

            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                first = c.Option("-1 --first <NAME>", "First argument", CommandOptionType.SingleValue);
                second = c.Option("-2 --second <NAME>", "Second argument", CommandOptionType.SingleValue);
                c.OnExecute(() => 0);
            });

            app.Execute("test", "-1=one", "-2", "two");

            Assert.Equal("one", first.Values[0]);
            Assert.Equal("two", second.Values[0]);
        }

        [Fact]
        public void ThrowsExceptionOnUnexpectedArgumentByDefault()
        {
            var unexpectedArg = "UnexpectedArg";
            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                c.OnExecute(() => 0);
            });

            var exception = Assert.Throws<Exception>(() => app.Execute("test", unexpectedArg));
            Assert.Equal(string.Format("TODO: Error: unrecognized {0} '{1}'", "argument", unexpectedArg),
                exception.Message);
        }

        [Fact]
        public void AllowNoThrowBehaviorOnUnexpectedArgument()
        {
            var unexpectedArg = "UnexpectedArg";
            var app = new CommandLineApplication();

            var testCmd = app.Command("test", c =>
            {
                c.OnExecute(() => 0);
            },
            addHelpCommand: false,
            throwOnUnexpectedArg: false);

            // (does not throw)
            app.Execute("test", unexpectedArg);
            Assert.Equal(1, testCmd.RemainingArguments.Count);
            Assert.Equal(unexpectedArg, testCmd.RemainingArguments[0]);
        }

        [Fact]
        public void ThrowsExceptionOnUnexpectedLongOptionByDefault()
        {
            var unexpectedOption = "--UnexpectedOption";
            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                c.OnExecute(() => 0);
            });

            var exception = Assert.Throws<Exception>(() => app.Execute("test", unexpectedOption));
            Assert.Equal(string.Format("TODO: Error: unrecognized {0} '{1}'", "option", unexpectedOption),
                exception.Message);
        }

        [Fact]
        public void AllowNoThrowBehaviorOnUnexpectedLongOption()
        {
            var unexpectedOption = "--UnexpectedOption";
            var app = new CommandLineApplication();

            var testCmd = app.Command("test", c =>
            {
                c.OnExecute(() => 0);
            },
            addHelpCommand: false,
            throwOnUnexpectedArg: false);

            // (does not throw)
            app.Execute("test", unexpectedOption);
            Assert.Equal(1, testCmd.RemainingArguments.Count);
            Assert.Equal(unexpectedOption, testCmd.RemainingArguments[0]);
        }

        [Fact]
        public void ThrowsExceptionOnUnexpectedShortOptionByDefault()
        {
            var unexpectedOption = "-uexp";
            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                c.OnExecute(() => 0);
            });

            var exception = Assert.Throws<Exception>(() => app.Execute("test", unexpectedOption));
            Assert.Equal(string.Format("TODO: Error: unrecognized {0} '{1}'", "option", unexpectedOption),
                exception.Message);
        }

        [Fact]
        public void AllowNoThrowBehaviorOnUnexpectedShortOption()
        {
            var unexpectedOption = "-uexp";
            var app = new CommandLineApplication();

            var testCmd = app.Command("test", c =>
            {
                c.OnExecute(() => 0);
            },
            addHelpCommand: false,
            throwOnUnexpectedArg: false);

            // (does not throw)
            app.Execute("test", unexpectedOption);
            Assert.Equal(1, testCmd.RemainingArguments.Count);
            Assert.Equal(unexpectedOption, testCmd.RemainingArguments[0]);
        }

        [Fact]
        public void ThrowsExceptionOnUnexpectedSymbolOptionByDefault()
        {
            var unexpectedOption = "-?";
            var app = new CommandLineApplication();

            app.Command("test", c =>
            {
                c.OnExecute(() => 0);
            });

            var exception = Assert.Throws<Exception>(() => app.Execute("test", unexpectedOption));
            Assert.Equal(string.Format("TODO: Error: unrecognized {0} '{1}'", "option", unexpectedOption),
                exception.Message);
        }

        [Fact]
        public void AllowNoThrowBehaviorOnUnexpectedSymbolOption()
        {
            var unexpectedOption = "-?";
            var app = new CommandLineApplication();

            var testCmd = app.Command("test", c =>
            {
                c.OnExecute(() => 0);
            },
            addHelpCommand: false,
            throwOnUnexpectedArg: false);

            // (does not throw)
            app.Execute("test", unexpectedOption);
            Assert.Equal(1, testCmd.RemainingArguments.Count);
            Assert.Equal(unexpectedOption, testCmd.RemainingArguments[0]);
        }

        [Fact]
        public void ThrowsExceptionOnUnexpectedOptionBeforeValidSubcommandByDefault()
        {
            var unexpectedOption = "--unexpected";
            CommandLineApplication subCmd = null;
            var app = new CommandLineApplication();

            app.Command("k", c =>
            {
                subCmd = c.Command("run", _=> { });
                c.OnExecute(() => 0);
            });

            var exception = Assert.Throws<Exception>(() => app.Execute("k", unexpectedOption, "run"));
            Assert.Equal(string.Format("TODO: Error: unrecognized {0} '{1}'", "option", unexpectedOption),
                exception.Message);
        }

        [Fact]
        public void AllowNoThrowBehaviorOnUnexpectedOptionAfterSubcommand()
        {
            var unexpectedOption = "--unexpected";
            CommandLineApplication subCmd = null;
            var app = new CommandLineApplication();

            var testCmd = app.Command("k", c =>
            {
                subCmd = c.Command("run", _ => { }, addHelpCommand: false, throwOnUnexpectedArg: false);
                c.OnExecute(() => 0);
            });

            // (does not throw)
            app.Execute("k", "run", unexpectedOption);
            Assert.Equal(0, testCmd.RemainingArguments.Count);
            Assert.Equal(1, subCmd.RemainingArguments.Count);
            Assert.Equal(unexpectedOption, subCmd.RemainingArguments[0]);
        }
        
        [Fact]
        public void HandlesAsyncExecutes()
        {
            bool called = false;

            var app = new CommandLineApplication();

            app.OnExecute(async () =>
            {
                await Task.Delay(5);
                called = true;
                return 2;
            });

            var result = app.Execute();

            Assert.True(called);
            Assert.Equal(2, result);
        }

        [Fact]
        public void PropagatesAsyncExceptions()
        {
            var app = new CommandLineApplication();

            app.OnExecute(async () =>
            {
                await Task.Delay(5);
                throw new InvalidOperationException("this should throw");
            });

            var ex = Assert.Throws<AggregateException>(() => app.Execute()).InnerException;
            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("this should throw", ex.Message);
        }
    }
}
