using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Builders;

namespace CameraH264ToMp4
{
    internal partial class FFmpeg
    {
        private readonly string _cliFilePath;

        public FFmpeg(string cliFilePath) => _cliFilePath = cliFilePath;

        public async Task ExecuteMuxAsync(string arguments, IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var stdErrBuffer = new StringBuilder();

            var stdErrPipe = PipeTarget.Merge(
                PipeTarget.ToStringBuilder(stdErrBuffer), // error data collector
                progress?.Pipe(p => new FFmpegProgressRouter(p)) ?? PipeTarget.Null // progress
            );

            var result = await Cli.Wrap(_cliFilePath)
                .WithArguments(arguments)
                .WithStandardErrorPipe(stdErrPipe)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"FFmpeg exited with a non-zero exit code ({result.ExitCode})." + Environment.NewLine +
                    "Standard error:" + Environment.NewLine +
                    stdErrBuffer
                );
            }
        }
    }

    internal partial class FFmpeg
    {
        private class FFmpegProgressRouter : PipeTarget
        {
            private readonly StringBuilder _buffer = new();
            private readonly IProgress<double> _output;

            private TimeSpan? _totalDuration;
            private TimeSpan? _lastOffset;

            public FFmpegProgressRouter(IProgress<double> output) => _output = output;

            private TimeSpan? TryParseTotalDuration(string data) => data
                .Pipe(s => Regex.Match(s, @"Duration:\s(\d\d:\d\d:\d\d.\d\d)").Groups[1].Value)
                .NullIfWhiteSpace()?
                .Pipe(s => TimeSpan.ParseExact(s, "c", CultureInfo.InvariantCulture));

            private TimeSpan? TryParseCurrentOffset(string data) => data
                .Pipe(s => Regex.Matches(s, @"time=(\d\d:\d\d:\d\d.\d\d)").Cast<Match>().LastOrDefault()?.Groups[1].Value)?
                .NullIfWhiteSpace()?
                .Pipe(s => TimeSpan.ParseExact(s, "c", CultureInfo.InvariantCulture));

            private void HandleBuffer()
            {
                var data = _buffer.ToString();

                int count = 0;
                try
                {
                    count = new Regex(Regex.Escape(data)).Matches("Last message repeated").Count;
                }
                catch (Exception ex)
                {

                }
                _output.Report(count);
            }

            public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
            {
                using var reader = new StreamReader(source, Console.OutputEncoding, false, 1024, true);

                var buffer = new char[1024];
                int charsRead;

                while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _buffer.Append(buffer, 0, charsRead);
                    HandleBuffer();
                }
            }
        }
    }

    internal static class GenericExtensions
    {
        public static TOut Pipe<TIn, TOut>(this TIn input, Func<TIn, TOut> transform) => transform(input);

        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T> =>
            value.CompareTo(min) <= 0
                ? min
                : value.CompareTo(max) >= 0
                    ? max
                    : value;
    }

    internal static class StringExtensions
    {
        public static string? NullIfWhiteSpace(this string s) =>
            !string.IsNullOrWhiteSpace(s)
                ? s
                : null;
    }
}
