using ControlShutter.Common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace ControlShutter.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ShutterController : ControllerBase
    {
        private readonly ILogger<ShutterController> _logger;

        Http http = new Http();

        public ShutterController(ILogger<ShutterController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public TaskRequest ControlShutterAsync([FromBody] TaskReceive receive)
        {
            TaskRequest taskRequest = new TaskRequest();
            try
            {
                Task.Run(async () =>
                {
                    switch (receive.taskType)
                    {
                        case TaskType.openDoor:
                            _logger.LogInformation("���յ���������");
                            OpenShutterTaskRequest openShutter = new OpenShutterTaskRequest();
                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.OpenDO(254, 0);
                            await Task.Delay(2000);

                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.CloseDO(254, 0);
                            _logger.LogInformation("�����ź�ȡ��");
                            Thread.Sleep(85000);
                            for (int i = 0; i < 10; i++)
                            {
                                if (ShutterClass.Instance.ReadDI(254, 2) != null)
                                {
                                    string rec = BitConverter.ToString(ShutterClass.Instance.ReadDI(254, 2));
                                    _logger.LogInformation($"���յ���ϢΪ��{rec}");
                                    if (rec == "01")
                                    {
                                        openShutter.code = 200;
                                        openShutter.msg = "success";
                                        _logger.LogInformation("��������ɹ����");
                                        openShutter.robotId = receive.robotId;
                                        openShutter.robotType = 2;
                                        openShutter.taskId = receive.taskId;
                                        http.PostJson("http://192.168.30.212:9093/luoshu-rcs/ApiAgvForWms/IntermediateTask", JsonConvert.SerializeObject(openShutter));
                                    }

                                    if (i == 9)
                                    {
                                        openShutter.code = 500;
                                        openShutter.msg = "fail";
                                        _logger.LogError($"�����������ʧ��");
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
                            _logger.LogInformation("���յ���������");
                            CloseShutterTaskRequest closeShutter = new CloseShutterTaskRequest();
                            closeShutter.startTime = DateTime.Now.ToString("yyyy-MMdd HH:mm:ss");
                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.OpenDO(254, 1);
                            await Task.Delay(2000);

                            ShutterClass.Instance.Connet();
                            ShutterClass.Instance.CloseDO(254, 1);
                            _logger.LogInformation("�����ź�ȡ��");
                            Thread.Sleep(85000);
                            for (int i = 0; i < 10; i++)
                            {
                                if (ShutterClass.Instance.ReadDI(254, 2) != null)
                                {
                                    string rec = BitConverter.ToString(ShutterClass.Instance.ReadDI(254, 2));
                                    _logger.LogInformation($"���յ���ϢΪ��{rec}");
                                    if (rec == "02")
                                    {
                                        closeShutter.executionStatus = 200;
                                        closeShutter.feedbackMsg = "success";
                                        _logger.LogInformation("��������ɹ����");
                                        closeShutter.robotId = receive.robotId;
                                        closeShutter.taskId = receive.taskId;
                                        closeShutter.endTime = DateTime.Now.ToString("yyyy-MMdd HH:mm:ss");
                                        http.PostJson("http://192.168.30.212:9093/luoshrcs/rcs/task/completionFeedback", JsonConvert.SerializeObject(closeShutter));
                                    }

                                    if (i == 9)
                                    {
                                        closeShutter.executionStatus = 500;
                                        closeShutter.feedbackMsg = "fail";
                                        _logger.LogError($"�����������ʧ��");
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
                            _logger.LogWarning($"�������������ʧ�ܣ�{receive.taskType}");
                            break;
                    }
                });
                taskRequest.status = 200;
                taskRequest.msg = "�����ͳɹ�";
                _logger.LogInformation($"����ID{receive.taskId}�·��ɹ�!");
                return taskRequest;
            }
            catch (Exception ex)
            {
                taskRequest.status = 500;
                taskRequest.msg = "������ʧ��";
                _logger.LogInformation($"����ID{receive.taskId}�·�ʧ��!ԭ��:{ex.Message}");
                return taskRequest;
            }
        }
    }
}