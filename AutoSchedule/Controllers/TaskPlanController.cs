﻿using AutoSchedule.Common;
using AutoSchedule.Dtos.Data;
using AutoSchedule.Dtos.Models;
using AutoSchedule.Dtos.RequestIn;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AutoSchedule.Controllers
{
    public class TaskPlanController : Controller
    {
        private IFreeSql _sqliteFSql;
        private SqlLiteContext _SqlLiteContext;
        private readonly ILogger<TaskPlanController> _logger;
        private QuartzStartup _quartzStartup;
        public TaskPlanController(ILogger<TaskPlanController> logger, SqlLiteContext SqlLiteContext, QuartzStartup quartzStartup, FreeSqlFactory freeSqlFactory)
        {
            _SqlLiteContext = SqlLiteContext;
            _logger = logger;
            _quartzStartup = quartzStartup;
            _sqliteFSql = freeSqlFactory.GetBaseSqlLite();
        }

        public IActionResult TaskPlan()
        {
            return View();
        }

        [HttpGet]
        public async Task<string> TaskPlanResult(int page, int limit)
        {
            var skeyAll = _SqlLiteContext.TaskPlan.AsNoTracking();
            var skey = await skeyAll.Skip((page - 1) * limit).Take(limit).ToListAsync();

            List<TaskPlanModel> data = new List<TaskPlanModel>();
            string OrgName = "";
            string FrequencyName = "";
            string FrequencyType = "";
            string WorkType = string.Empty;
            foreach (var item in skey)
            {
                string Status = string.Empty;
                if (string.IsNullOrEmpty(item.OrgCode))
                {
                    OrgName = "";
                }
                else
                {
                    OrgName = (await _SqlLiteContext.OrgSetting.AsNoTracking().FirstAsync(o => o.CODE == item.OrgCode)).NAME;
                }

                if (item.FrequencyType == "0")
                {
                    FrequencyType = "秒";
                }
                else if (item.FrequencyType == "1")
                {
                    FrequencyType = "分钟";
                }
                else if (item.FrequencyType == "2")
                {
                    FrequencyType = "小时";
                }


                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {

                    if (_quartzStartup.rds.ContainsKey(item.GUID))
                    {
                        Status = "已开启";
                    }
                    else
                    {
                        Status = "未开启";
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (item.Status == "1")
                    {
                        Status = "已开启";
                    }
                    else
                    {
                        Status = "未开启";
                    }
                }

                
                

                if (item.WorkType == "0")
                {
                    WorkType = "发送数据到dll";
                }
                else
                {
                    WorkType = "发送数据到api";
                }

                FrequencyName = $"每隔{item.Frequency + FrequencyType}执行一次任务";
                data.Add(new TaskPlanModel { GUID = item.GUID, CODE = item.CODE, Name = item.Name, OrgCode = OrgName, Frequency = FrequencyName, DllOrUrl = item.DllOrUrl, Status = Status, WorkType = WorkType });
            }


            return System.Text.Json.JsonSerializer.Serialize(new TaskPlanData { msg = "", count = skeyAll.Count(), code = 0, data = data });
        }

        [HttpGet]
        //http://localhost:5000/TaskPlan/TaskPlanDetailResult?guid=100&page=1&limit=10
        public async Task<string> TaskPlanDetailResult(TaskPlanRequest taskPlanRequest)
        {
            var skey = await _SqlLiteContext.TaskPlanRelation.AsNoTracking().Where(o => o.TaskPlanGuid == taskPlanRequest.guid).ToListAsync();
            List<TaskPlanDetailModel> data = new List<TaskPlanDetailModel>();
            string dsName = "";
            string dsState = "";
            foreach (var item in skey)
            {
                dsName = (await _SqlLiteContext.OpenSql.AsNoTracking().FirstAsync(o => o.GUID == item.OpenSqlGuid)).Name;
                dsState = (await _SqlLiteContext.OpenSql.AsNoTracking().FirstAsync(o => o.GUID == item.OpenSqlGuid)).IsStart;

                data.Add(new TaskPlanDetailModel { dsGuid = item.OpenSqlGuid, dsName = dsName, dsState = dsState, tkDetailGuid = item.GUID });
            }
            return System.Text.Json.JsonSerializer.Serialize(new TaskPlanDetailData { msg = "", count = data.Count, code = 0, data = data });
        }

        public IActionResult TaskPlanAdd()
        {
            return View();
        }

        [HttpPost]
        //string FType, string GUID, string Name, string IsStart, string MainKey, string GroupSqlString, string SqlString, string AfterSqlString, string AfterSqlstring2
        public async Task<string> TaskPlanAdd([FromBody]TaskPlanExGuidCode TaskPlanAddIn)
        {
            string guid = Guid.NewGuid().ToString();
            string code = "";
            var isExist = await _SqlLiteContext.TaskPlan.AsNoTracking().OrderByDescending(o => o.CODE).FirstOrDefaultAsync();
            if (isExist == null)
            {
                code = "100";
            }
            else
            {
                code = (int.Parse(isExist.CODE) + 1).ToString();
            }
            Dtos.Models.TaskPlan TaskPlanAdd = new Dtos.Models.TaskPlan
            {
                CODE = code,
                Frequency = TaskPlanAddIn.Frequency,
                FrequencyType = TaskPlanAddIn.FrequencyType,
                GUID = guid,
                Name = TaskPlanAddIn.Name,
                OrgCode = TaskPlanAddIn.OrgCode,
                DllOrUrl = TaskPlanAddIn.DllOrUrl,
                WorkType = TaskPlanAddIn.WorkType
            };
            await _SqlLiteContext.TaskPlan.AddAsync(TaskPlanAdd);
            var addresult = await _SqlLiteContext.SaveChangesAsync();
            if (addresult > 0)
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "", code = "0" });
            }
            else
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "新增任务计划失败", code = "-1" });
            }
        }

        public async Task<IActionResult> TaskPlanEdit(string GUID)
        {
            var ds = await _SqlLiteContext.TaskPlan.AsNoTracking().Where(o => o.GUID == GUID).FirstOrDefaultAsync();
            return View(ds);
        }

        [HttpPost]
        public async Task<string> TaskPlanEdit([FromBody]TaskPlan TaskPlanIn)
        {
            if (_sqliteFSql.Select<TaskPlan>().Where(o=>o.GUID == TaskPlanIn.GUID&&o.Status=="1").ToList().Count !=0)
            {
                return new ResponseCommon { msg = "此任务计划正在执行，不允许修改！", code = "1" }.ToJsonCommon();
            }

            var dsuptate = await _SqlLiteContext.TaskPlan.Where(o => o.CODE == TaskPlanIn.CODE).FirstOrDefaultAsync();
            dsuptate.Frequency = TaskPlanIn.Frequency;
            dsuptate.FrequencyType = TaskPlanIn.FrequencyType;
            dsuptate.Name = TaskPlanIn.Name;
            dsuptate.OrgCode = TaskPlanIn.OrgCode;
            dsuptate.DllOrUrl = TaskPlanIn.DllOrUrl;
            dsuptate.WorkType = TaskPlanIn.WorkType;
            _SqlLiteContext.TaskPlan.Update(dsuptate);
           var addresult = await _SqlLiteContext.SaveChangesAsync();
            if (addresult > 0)
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "修改任务计划成功", code = "0" });
            }
            else
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "修改任务计划失败", code = "-1" });
            }
        }

        [HttpGet]
        public async Task<string> TaskPlanDelete(string GUID)
        {
            if (_sqliteFSql.Select<TaskPlan>().Where(o => o.GUID == GUID && o.Status == "1").ToList().Count != 0)
            {
                return new ResponseCommon { msg = "此任务计划正在执行，不允许删除！", code = "1" }.ToJsonCommon();
            }


            var orgdelete = await _SqlLiteContext.TaskPlan.AsNoTracking().Where(o => o.GUID == GUID).FirstOrDefaultAsync();

            var tkdetailrembe = await _SqlLiteContext.TaskPlanRelation.AsNoTracking().Where(o => o.TaskPlanGuid == GUID).ToListAsync();

            _SqlLiteContext.TaskPlanRelation.RemoveRange(tkdetailrembe);
            _SqlLiteContext.TaskPlan.Remove(orgdelete);

            if (await _SqlLiteContext.SaveChangesAsync() > 0)
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "", code = "0" });
            }
            else
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "删除任务计划失败", code = "-1" });
            }
        }

        public async Task<IActionResult> TaskPlanDetailAdd(string tkguid)
        {
            var ds = await _SqlLiteContext.TaskPlan.AsNoTracking().Where(o => o.GUID == tkguid).FirstOrDefaultAsync();
            return View(ds);
        }

        [HttpPost]
        //string FType, string GUID, string Name, string IsStart, string MainKey, string GroupSqlString, string SqlString, string AfterSqlString, string AfterSqlstring2
        public async Task<string> TaskPlanDetailAdd([FromBody]TaskPlanDetailExGuid TaskPlanDetailExGuidAddIn)
        {
            string guid = "";
            var isExist = await _SqlLiteContext.TaskPlanRelation.AsNoTracking().OrderByDescending(o => o.GUID).FirstOrDefaultAsync();
            if (isExist == null)
            {
                guid = "100";
            }
            else
            {
                guid = (int.Parse(isExist.GUID) + 1).ToString();
            }
            Dtos.Models.TaskPlanDetail TaskPlanDetailAdd = new Dtos.Models.TaskPlanDetail
            {
                GUID = guid,
                OpenSqlGuid = TaskPlanDetailExGuidAddIn.OpenSqlGuid,
                TaskPlanGuid = TaskPlanDetailExGuidAddIn.TaskPlanGuid
            };
            await _SqlLiteContext.TaskPlanRelation.AddAsync(TaskPlanDetailAdd);
            var addresult = await _SqlLiteContext.SaveChangesAsync();
            if (addresult > 0)
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "", code = "0" });
            }
            else
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "新增任务计划明细失败", code = "-1" });
            }
        }

        [HttpGet]
        public async Task<string> TaskPlanDetailDelete(string GUID)
        {
            var tkdetaildelete = await _SqlLiteContext.TaskPlanRelation.AsNoTracking().Where(o => o.GUID == GUID).FirstOrDefaultAsync();

            _SqlLiteContext.TaskPlanRelation.Remove(tkdetaildelete);

            if (await _SqlLiteContext.SaveChangesAsync() > 0)
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "", code = "0" });
            }
            else
            {
                return System.Text.Json.JsonSerializer.Serialize(new ResponseCommon { msg = "删除任务计划明细失败", code = "-1" });
            }
        }

       
    }
}