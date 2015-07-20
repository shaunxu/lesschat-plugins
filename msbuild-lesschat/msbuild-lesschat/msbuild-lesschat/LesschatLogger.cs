using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace msbuild_lesschat
{
    public class LesschatLogger : Logger
    {
        private readonly StringBuilder _sb;
        private int _indent;
        private LesschatClient _client;

        public LesschatLogger() : base()
        {
            _sb = new StringBuilder();
            _indent = 0;
            _client = null;
        }

        public override void Initialize(IEventSource eventSource)
        {
            if (Parameters == null)
            {
                throw new LoggerException("Parameter was not specified.");
            }

            var parameters = Parameters.Split(';');
            if (parameters.Length <= 0)
            {
                throw new LoggerException("Parameter was not specified.");
            }

            var webhook = parameters[0].Trim();
            if (string.IsNullOrWhiteSpace(webhook))
            {
                throw new LoggerException("Lesschat incoming message webhook URL was not specified.");
            }

            Uri webhookUri;
            if (!Uri.TryCreate(webhook, UriKind.Absolute, out webhookUri))
            {
                if (!Uri.TryCreate(string.Format("https://hook.lesschat.com/incoming/{0}", webhook), UriKind.Absolute, out webhookUri))
                {
                    throw new LoggerException(string.Format("Invalid incoming message webhook URL ({0}).", webhook));
                }
            }
            _client = new LesschatClient(webhookUri);

            eventSource.BuildStarted += (sender, e) =>
             {
                 _sb.AppendLine(GenerateMessage(string.Empty, e, "BuildStarted"));
                 _indent++;
             };

            eventSource.BuildFinished += (sender, e) =>
            {
                _indent--;
                _sb.AppendLine(GenerateMessage(string.Empty, e, "BuildFinished"));
            };

            eventSource.ProjectStarted += (sender, e) =>
              {
                  _sb.AppendLine(GenerateMessage(string.Empty, e, "ProjectStarted"));
                  _indent++;
              };

            eventSource.ProjectFinished += (sender, e) =>
              {
                  _indent--;
                  _sb.AppendLine(GenerateMessage(string.Empty, e, "ProjectFinished"));
              };

            eventSource.TaskStarted += (sender, e) =>
              {
                  _sb.AppendLine(GenerateMessage(string.Empty, e, "TaskStarted"));
                  _indent++;
              };

            eventSource.TaskFinished += (sender, e) =>
            {
                _indent--;
                _sb.AppendLine(GenerateMessage(string.Empty, e, "TaskFinished"));
            };

            eventSource.TargetStarted += (sender, e) =>
            {
                _sb.AppendLine(GenerateMessage(string.Empty, e, "TargetStarted"));
                _indent++;
            };

            eventSource.TargetFinished += (sender, e) =>
            {
                _indent--;
                _sb.AppendLine(GenerateMessage(string.Empty, e, "TargetFinished"));
            };

            eventSource.ErrorRaised += (sender, e) =>
              {
                  var line = string.Format(": Error {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
                  _sb.AppendLine(GenerateMessage(line, e, "ErrorRaised"));
              };

            eventSource.WarningRaised += (sender, e) =>
            {
                var line = string.Format(": Warning {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
                _sb.AppendLine(GenerateMessage(line, e, "WarningRaised"));
            };

            eventSource.MessageRaised += (sender, e) =>
              {
                  if ((e.Importance == MessageImportance.High && IsVerbosityAtLeast(LoggerVerbosity.Minimal))
                    || (e.Importance == MessageImportance.Normal && IsVerbosityAtLeast(LoggerVerbosity.Normal))
                    || (e.Importance == MessageImportance.Low && IsVerbosityAtLeast(LoggerVerbosity.Detailed)))
                  {
                      _sb.AppendLine(GenerateMessage(string.Empty, e, "MessageRaised"));
                  }
              };
        }

        private string GenerateMessage(string line, BuildEventArgs e, string category)
        {
            var title = string.Compare(e.SenderName, "MSBuild", true) == 0 ? line : string.Format("{0}: {1}", e.SenderName, line);
            var result = string.Empty;
            for (int i = _indent; i > 0; i--)
            {
                result += "    ";
            }
            result += category + " > ";
            result += title;
            result += e.Message;
            return result;
        }

        private void OutputMessages(string message)
        {
            Console.WriteLine(message);

            //var response = _client.SendAsync(message).Result;
            //Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
        }

        public override void Shutdown()
        {
            OutputMessages(_sb.ToString());

            base.Shutdown();
        }
    }
}
