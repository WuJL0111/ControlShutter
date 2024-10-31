namespace ControlShutter.Common
{
    public class TaskRequest
    {
        public int status { get; set; }

        public string msg { get; set; }
    }

    public class OpenShutterTaskRequest
    {
        public int code { get; set; }

        public string msg { get; set; }

        public long robotId { get; set; }

        public int robotType { get; set; }

        public long taskId { get; set; }
    }

    public class CloseShutterTaskRequest
    {
        public long robotId { get; set; }

        public long taskId { get; set; }

        public double weight { get; set; }

        public string scanInfo { get; set; }

        public bool visionResult { get; set; }

        public List<string> rfidResult { get; set; }

        public int executionStatus { get; set; }

        public string feedbackMsg { get; set; }

        public string startTime { get; set; }

        public string endTime { get; set; }
    }
}