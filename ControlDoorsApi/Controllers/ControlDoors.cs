using ControlDoorsApi.Common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ControlDoorsApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ControlDoors : ControllerBase
    {
        private readonly ILogger<ControlDoors> _logger;

        Http http = new Http();

        public ControlDoors(ILogger<ControlDoors> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public TaskRequest ControlPullDoor([FromBody] TaskReceive receive)
        {
            TaskRequest request = new TaskRequest();
            try
            {
                Task.Run(async () =>
                {
                    switch (receive.taskType)
                    {
                        case TaskType.openDoor:
                            _logger.LogInformation("接收到开门任务");
                            OpenShutterTaskRequest openShutter = new OpenShutterTaskRequest();
                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.OpenDO(254, 0);
                            await Task.Delay(2000);

                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.CloseDO(254, 0);
                            _logger.LogInformation("开门信号取消");
                            Thread.Sleep(85000);
                            for (int i = 0; i < 10; i++)
                            {
                                if (ShutterClass.Instance.ReadDI(254, 2) != null)
                                {
                                    string rec = BitConverter.ToString(ShutterClass.Instance.ReadDI(254, 2));
                                    _logger.LogInformation($"接收到消息为：{rec}");
                                    if (rec == "01")
                                    {
                                        openShutter.code = 200;
                                        openShutter.msg = "success";
                                        _logger.LogInformation("开门任务成功完成");
                                        openShutter.robotId = receive.robotId;
                                        openShutter.robotType = 2;
                                        openShutter.taskId = receive.taskId;
                                        http.PostJson("http://192.168.30.212:9093/luoshu-rcs/ApiAgvForWms/IntermediateTask", JsonConvert.SerializeObject(openShutter));
                                    }

                                    if (i == 9)
                                    {
                                        openShutter.code = 500;
                                        openShutter.msg = "fail";
                                        _logger.LogError($"开门任务完成失败");
                                        openShutter.robotId = receive.robotId;
                                        openShutter.robotType = 2;
                                        openShutter.taskId = receive.taskId;
                                        http.PostJson("http://192.168.30.212:9093/luoshu-rcs/ApiAgvForWms/IntermediateTask", JsonConvert.SerializeObject(openShutter));
                                    }
                                }
                                Thread.Sleep(3000);
                            }
                            break;
                        case TaskType.closeDoor:
                            _logger.LogInformation("接收到关门任务");
                            CloseShutterTaskRequest closeShutter = new CloseShutterTaskRequest();
                            closeShutter.startTime = DateTime.Now.ToString("yyyy-MMdd HH:mm:ss");
                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.OpenDO(254, 1);
                            await Task.Delay(2000);

                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.CloseDO(254, 1);
                            _logger.LogInformation("关门信号取消");
                            Thread.Sleep(85000);
                            for (int i = 0; i < 10; i++)
                            {
                                if (ShutterClass.Instance.ReadDI(254, 2) != null)
                                {
                                    string rec = BitConverter.ToString(ShutterClass.Instance.ReadDI(254, 2));
                                    _logger.LogInformation($"接收到消息为：{rec}");
                                    if (rec == "02")
                                    {
                                        closeShutter.executionStatus = 200;
                                        closeShutter.feedbackMsg = "success";
                                        _logger.LogInformation("关门任务成功完成");
                                        closeShutter.robotId = receive.robotId;
                                        closeShutter.taskId = receive.taskId;
                                        closeShutter.endTime = DateTime.Now.ToString("yyyy-MMdd HH:mm:ss");
                                        http.PostJson("http://192.168.30.212:9093/luoshrcs/rcs/task/completionFeedback", JsonConvert.SerializeObject(closeShutter));
                                    }

                                    if (i == 9)
                                    {
                                        closeShutter.executionStatus = 500;
                                        closeShutter.feedbackMsg = "fail";
                                        _logger.LogError($"关门任务完成失败");
                                        closeShutter.robotId = receive.robotId;
                                        closeShutter.taskId = receive.taskId;
                                        closeShutter.endTime = DateTime.Now.ToString("yyyy-MMdd HH:mm:ss");
                                        http.PostJson("http://192.168.30.212:9093/luoshu-rcs/rcs/task/completionFeedback", JsonConvert.SerializeObject(closeShutter));
                                    }
                                }
                                Thread.Sleep(3000);
                            }
                            break;
                        default:
                            _logger.LogWarning($"卷帘门开关门任务解析失败，{receive.taskType}");
                            break;
                    }
                });

                request.status = 200;
                request.msg = "任务发送成功";
                _logger.LogInformation($"任务ID{receive.taskId}下发成功!");
                return request;
            }
            catch(Exception ex)
            {
                request.status = 500;
                request.msg = "任务发送失败";
                _logger.LogInformation($"任务ID{receive.taskId}下发失败!原因:{ex.Message}");
                return request;
            }
        }

        [HttpPost]
        public TaskRequest ControlShutterDoor([FromBody] TaskReceive receive)
        {
            TaskRequest request = new TaskRequest();
            try
            {
                Task.Run(async () =>
                {
                    switch (receive.taskType)
                    {
                        case TaskType.openDoor:
                            _logger.LogInformation("接收到开门任务");
                            OpenShutterTaskRequest openShutter = new OpenShutterTaskRequest();
                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.OpenDO(254, 0);
                            await Task.Delay(2000);

                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.CloseDO(254, 0);
                            _logger.LogInformation("开门信号取消");
                            Thread.Sleep(85000);
                            for (int i = 0; i < 10; i++)
                            {
                                if (ShutterClass.Instance.ReadDI(254, 2) != null)
                                {
                                    string rec = BitConverter.ToString(ShutterClass.Instance.ReadDI(254, 2));
                                    _logger.LogInformation($"接收到消息为：{rec}");
                                    if (rec == "01")
                                    {
                                        openShutter.code = 200;
                                        openShutter.msg = "success";
                                        _logger.LogInformation("开门任务成功完成");
                                        openShutter.robotId = receive.robotId;
                                        openShutter.robotType = 2;
                                        openShutter.taskId = receive.taskId;
                                        http.PostJson("http://192.168.30.212:9093/luoshu-rcs/ApiAgvForWms/IntermediateTask", JsonConvert.SerializeObject(openShutter));
                                    }

                                    if (i == 9)
                                    {
                                        openShutter.code = 500;
                                        openShutter.msg = "fail";
                                        _logger.LogError($"开门任务完成失败");
                                        openShutter.robotId = receive.robotId;
                                        openShutter.robotType = 2;
                                        openShutter.taskId = receive.taskId;
                                        http.PostJson("http://192.168.30.212:9093/luoshu-rcs/ApiAgvForWms/IntermediateTask", JsonConvert.SerializeObject(openShutter));
                                    }
                                }
                                Thread.Sleep(3000);
                            }
                            break;
                        case TaskType.closeDoor:
                            _logger.LogInformation("接收到关门任务");
                            CloseShutterTaskRequest closeShutter = new CloseShutterTaskRequest();
                            closeShutter.startTime = DateTime.Now.ToString("yyyy-MMdd HH:mm:ss");
                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.OpenDO(254, 1);
                            await Task.Delay(2000);

                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.CloseDO(254, 1);
                            _logger.LogInformation("关门信号取消");
                            Thread.Sleep(85000);
                            for (int i = 0; i < 10; i++)
                            {
                                if (ShutterClass.Instance.ReadDI(254, 2) != null)
                                {
                                    string rec = BitConverter.ToString(ShutterClass.Instance.ReadDI(254, 2));
                                    _logger.LogInformation($"接收到消息为：{rec}");
                                    if (rec == "02")
                                    {
                                        closeShutter.executionStatus = 200;
                                        closeShutter.feedbackMsg = "success";
                                        _logger.LogInformation("关门任务成功完成");
                                        closeShutter.robotId = receive.robotId;
                                        closeShutter.taskId = receive.taskId;
                                        closeShutter.endTime = DateTime.Now.ToString("yyyy-MMdd HH:mm:ss");
                                        http.PostJson("http://192.168.30.212:9093/luoshrcs/rcs/task/completionFeedback", JsonConvert.SerializeObject(closeShutter));
                                    }

                                    if (i == 9)
                                    {
                                        closeShutter.executionStatus = 500;
                                        closeShutter.feedbackMsg = "fail";
                                        _logger.LogError($"关门任务完成失败");
                                        closeShutter.robotId = receive.robotId;
                                        closeShutter.taskId = receive.taskId;
                                        closeShutter.endTime = DateTime.Now.ToString("yyyy-MMdd HH:mm:ss");
                                        http.PostJson("http://192.168.30.212:9093/luoshu-rcs/rcs/task/completionFeedback", JsonConvert.SerializeObject(closeShutter));
                                    }
                                }
                                Thread.Sleep(3000);
                            }
                            break;
                        default:
                            _logger.LogWarning($"卷帘门开关门任务解析失败，{receive.taskType}");
                            break;
                    }
                });

                request.status = 200;
                request.msg = "任务发送成功";
                _logger.LogInformation($"任务ID{receive.taskId}下发成功!");
                return request;
            }
            catch (Exception ex)
            {
                request.status = 500;
                request.msg = "任务发送失败";
                _logger.LogInformation($"任务ID{receive.taskId}下发失败!原因:{ex.Message}");
                return request;
            }
        }
    }
}