using AutoTask.Model;
using BankDbHelper;
using ExcuteInterface;
using Jdwl.Api;
using Newtonsoft.Json;
using OpenApiSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoTask
{
    public class UpJob : IUpJob
    {
        JobLogger _jobLogger;
        public UpJob(JobLogger jobLogger)
        {
            _jobLogger = jobLogger;
        }
        private SqlHelper sqlHelper;
        private IJdClient client;
        private string serverUrl;
        private string accessToken;
        private string appKey;
        private string appSecret;
        private string Pin;
        private string wdgj_appkey;
        private string wdgj_appsecret;
        private string wdgj_accesstoken;
        private string createdate;
        public async Task<int> ExecJob(JobPara jobPara, List<Datas> dsData)
        {
            sqlHelper = new SqlHelper(jobPara.dbType, jobPara.connString);
            serverUrl = dsData[0].DataMain.Rows[0].DataRowGetStringValue("serverUrl");
            accessToken = dsData[0].DataMain.Rows[0].DataRowGetStringValue("accessToken");
            appKey = dsData[0].DataMain.Rows[0].DataRowGetStringValue("appKey");
            appSecret = dsData[0].DataMain.Rows[0].DataRowGetStringValue("appSecret");
            Pin = dsData[0].DataMain.Rows[0].DataRowGetStringValue("Pin");
            wdgj_appkey = dsData[0].DataMain.Rows[0].DataRowGetStringValue("wdgj_appkey");
            wdgj_appsecret = dsData[0].DataMain.Rows[0].DataRowGetStringValue("wdgj_appsecret");
            wdgj_accesstoken = dsData[0].DataMain.Rows[0].DataRowGetStringValue("wdgj_accesstoken");
            createdate = jobPara.dbType == DBTypeEnum.Oracle.ToString() ? "sysdate" : "getdate()";
            var result = jobPara.jobCode switch
            {
                "10001" => await SupplierInfo(jobPara, dsData),
                "10002" => await DrugInfo(jobPara, dsData),
                "20002" => await DrugInfo(jobPara, dsData),
                "20001" => await QueryDrug(jobPara, dsData),
                "resetError" => await ResetError(jobPara, dsData),
                "10012" => await WdgjSupplier(jobPara, dsData),
                "10011" => await WdgjGoods(jobPara, dsData),
                "10010" => await OrderClose(jobPara, dsData),
                "10003" => await ShopInfo(jobPara, dsData),
                "10004" => await PurchaseOrder(jobPara, dsData),
                "10005" => await QueryPurchase(jobPara, dsData),
                "10006" => await PurchaseReturn(jobPara, dsData),
                "10007" => await QueryPurchaseReturn(jobPara, dsData),
                "10008" => await QueryStock(jobPara, dsData),
                "10009" => await DBRK(jobPara, dsData),
                "10013" => await DBCK(jobPara, dsData),
                _ => -1,
            };
            return result;
        }

        /// <summary>
        /// 调拨出库
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> DBCK(JobPara jobPara, List<Datas> dsData)
        {
            try
            {
                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "调拨出库信息推送", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return 0;
                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }

                for (int i = 0; i < dsData.Count; i++)
                {
                    var OrderLines = new List<Jdwl.Api.Domain.Clps.ClpsOpenGwService.SoOrderLine>();
                    for (int j = 0; j < dsData[i].DataDetail[0].Rows.Count; j++)
                    {
                        OrderLines.Add(new Jdwl.Api.Domain.Clps.ClpsOpenGwService.SoOrderLine
                        {
                            OrderLineNo = dsData[i].DataDetail[0].Rows[j]["DETAILNO"].SqlDataBankToString(),
                            ItemId = dsData[i].DataDetail[0].Rows[j]["CLPSGOODSCODE"].SqlDataBankToString(),
                            PlanQty = dsData[i].DataDetail[0].Rows[j]["QUANTITY"].SqlDataBankToInt()
                            ///,ProduceCode = dsData[i].DataDetail[0].Rows[j]["ProduceCode"].SqlDataBankToString()
                        });
                    }

                    var request = new Jdwl.Api.Request.Clps.ClpsTransportSoOrderLopRequest
                    {
                        Pin = Pin,
                        Request = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.SoCreateRequest
                        {
                            DeliveryOrder = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.SoDeliveryOrder
                            {
                                DeliveryOrderCode = dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString(),
                                IsvSource = dsData[i].DataMain.Rows[0]["IsvSource"].SqlDataBankToString(),
                                OrderType = dsData[i].DataMain.Rows[0]["ORDERTYPE"].SqlDataBankToString(),
                                SoType = "1",
                                WarehouseCode = dsData[i].DataMain.Rows[0]["warehouseNo"].SqlDataBankToString(),
                                OrderMark = "0",
                                SourcePlatformCode = "1",
                                OwnerNo = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                                ShopNo = dsData[i].DataMain.Rows[0]["ShopNo"].SqlDataBankToString(),
                                LogisticsCode = dsData[i].DataMain.Rows[0]["LogisticsCode"].SqlDataBankToString(),
                                ReceiverInfo = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.ReceiverInfo
                                {
                                    Name = dsData[i].DataMain.Rows[0]["receiverName"].SqlDataBankToString(),
                                    Mobile = dsData[i].DataMain.Rows[0]["receiverMobile"].SqlDataBankToString(),
                                    DetailAddress = dsData[i].DataMain.Rows[0]["receiverAddr"].SqlDataBankToString()
                                }
                            },
                            OrderLines = OrderLines
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);
                    //{"response":{"content":{"code":"1","createTime":"2020-04-09 14:11:02","entryOrderCode":"CPL4418047912171","flag":"success","message":"成功"}, "code":0}}
                    var response = client.Execute(request);
                    await _jobLogger.WriteLogAsync(LogType.Info, "调拨出库信息推送", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    DBCKDResponseBody returnValue = JsonConvert.DeserializeObject<DBCKDResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        if (returnValue.response.content.flag == "success")
                        {
                            var insertresult = await sqlHelper.ExecSqlAsync($"insert into CKDBDZK_XH_JDWMS(LSH,SYNCDATE,DBRKCKDCODE,TYPE) values('{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}',{createdate},'{returnValue.response.content.deliveryOrderCode}','{dsData[i].DataMain.Rows[0]["ORDERTYPE"].SqlDataBankToString()}')");
                            if (string.IsNullOrEmpty(insertresult))
                            {
                                await _jobLogger.WriteLogAsync(LogType.Info, "调拨出库信息推送", $"{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}推送成功!");
                            }
                            else
                            {
                                await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()},'dbckd','LSH',{createdate},'{returnValue.response.content.message}')");
                                await _jobLogger.WriteLogAsync(LogType.Info, "调拨出库信息推送", $"{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}推送失败，失败原因:\r\n{returnValue.response.content.message + insertresult}");
                            }
                        }
                        else
                        {
                            await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()},'dbckd','LSH',{createdate},'{returnValue.response.content.message}')");
                            await _jobLogger.WriteLogAsync(LogType.Info, "调拨出库信息推送", $"{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}推送失败，失败原因:\r\n{returnValue.response.content.message}");
                        }
                    }
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "调拨出库信息推送", $"{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}推送失败，返回值:\r\n{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "调拨出库信息推送", $"失败，\r\n{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 调拨入库
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> DBRK(JobPara jobPara, List<Datas> dsData)
        {
            try
            {
                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "调拨入库信息推送", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }
                for (int i = 0; i < dsData.Count; i++)
                {
                    var OrderLines = new List<Jdwl.Api.Domain.Clps.ClpsOpenGwService.PoItemModel>();
                    for (int j = 0; j < dsData[i].DataDetail[0].Rows.Count; j++)
                    {
                        OrderLines.Add(new Jdwl.Api.Domain.Clps.ClpsOpenGwService.PoItemModel
                        {
                            OrderLineNo = dsData[i].DataDetail[0].Rows[j]["DETAILNO"].SqlDataBankToString(),
                            ItemNo = dsData[i].DataDetail[0].Rows[j]["CLPSGOODSCODE"].SqlDataBankToString(),
                            PlanQty = dsData[i].DataDetail[0].Rows[j]["QUANTITY"].SqlDataBankToInt(),
                            GoodsStatus = "1",
                            ProduceCode = dsData[i].DataDetail[0].Rows[j]["ProduceCode"].SqlDataBankToString()
                        });
                    }

                    var request = new Jdwl.Api.Request.Clps.ClpsAddPoOrderLopRequest
                    {
                        Pin = Pin,
                        PoAddRequest = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.PoAddRequest
                        {
                            EntryOrder = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.PoModel
                            {
                                EntryOrderCode = dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString(),
                                OwnerCode = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                                //the supplier code in JdWms
                                SupplierCode = dsData[i].DataMain.Rows[0]["SupplierCode"].SqlDataBankToString(),
                                WarehouseCode = dsData[i].DataMain.Rows[0]["warehouseNo"].SqlDataBankToString(),
                                OrderType = dsData[i].DataMain.Rows[0]["ORDERTYPE"].SqlDataBankToString()
                                //RelatedOrderList = new List<Jdwl.Api.Domain.Clps.ClpsOpenGwService.RelatedOrder> {
                                //    new Jdwl.Api.Domain.Clps.ClpsOpenGwService.RelatedOrder
                                //    {
                                //        OrderCode =  dsData[i].DataMain.Rows[0]["ORDERTYPE"].SqlDataBankToString(),
                                //        OrderType =  dsData[i].DataMain.Rows[0]["ORDERTYPE"].SqlDataBankToString()
                                //    }
                                //}
                            },

                            OrderLines = OrderLines
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);
                    //{"response":{"content":{"code":"1","createTime":"2020-04-09 14:11:02","entryOrderCode":"CPL4418047912171","flag":"success","message":"成功"}, "code":0}}
                    var response = client.Execute(request);
                    await _jobLogger.WriteLogAsync(LogType.Info, "调拨入库信息推送", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    OrderResponseBody returnValue = JsonConvert.DeserializeObject<OrderResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        if (returnValue.response.content.flag == "success")
                        {
                            //save rtsorderstatus to STOCKRETURNAPPROVE_XH_JDWMS table
                            var insertresult = await sqlHelper.ExecSqlAsync($"insert into CKDBDZK_XH_JDWMS(LSH,SYNCDATE,DBRKCKDCODE,TYPE) values('{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}',{createdate},'{returnValue.response.content.entryOrderCode}','{dsData[i].DataMain.Rows[0]["ORDERTYPE"].SqlDataBankToString()}')");
                            if (string.IsNullOrEmpty(insertresult))
                            {
                                await _jobLogger.WriteLogAsync(LogType.Info, "调拨入库信息推送", $"{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}推送成功!");
                            }
                            else
                            {
                                await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()},'dbrkd','LSH',{createdate},'{returnValue.response.content.message}')");
                                await _jobLogger.WriteLogAsync(LogType.Info, "调拨入库信息推送", $"{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}推送失败，失败原因:\r\n{returnValue.response.content.message + insertresult}");
                            }

                        }
                        else
                        {
                            await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()},'dbrkd','LSH',{createdate},'{returnValue.response.content.message}')");
                            await _jobLogger.WriteLogAsync(LogType.Info, "调拨入库信息推送", $"{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}推送失败，失败原因:\r\n{returnValue.response.content.message}");
                        }


                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "调拨入库信息推送", $"{dsData[i].DataMain.Rows[0]["LSH"].SqlDataBankToString()}推送失败，返回值:\r\n{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "调拨入库信息推送", $"失败，\r\n{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 查询库存
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> QueryStock(JobPara jobPara, List<Datas> dsData)
        {
            try
            {


                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "查询库存信息", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }
                for (int i = 0; i < dsData.Count; i++)
                {
                    var request = new Jdwl.Api.Request.Clps.ClpsQueryStockLopRequest
                    {
                        Pin = Pin,
                        QueryWarehouseStockRequest = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.QueryWarehouseStockRequest
                        {
                            WarehouseNo = dsData[i].DataMain.Rows[0]["warehouseNo"].SqlDataBankToString(),
                            OwnerNo = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                            GoodsNo = dsData[i].DataMain.Rows[0]["CLPSGOODSCODE"].SqlDataBankToString(),
                            CurrentPage = 1,
                            PageSize = 1
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);


                    //{"response":{"content":{"code":"1","flag":"success","message":"成功","totalLines":1,"warehouseStockModelList":[{"goodsName":"耳聋左慈丸","goodsNo":"CMG4418287716460","ownerName":"GXW测试货主勿动","ownerNo":"CBU8816093026319","sellerGoodsSign":"00000001","stockStatus":"1","stockType":"1","totalNum":2000,"totalNumValue":2000.0,"usableNum":2000,"usableNumValue":2000.0,"warehouseName":"GXWj接口测试仓","warehouseNo":"800001801"}]}, "code":0}}
                    var response = client.Execute(request);
                    await _jobLogger.WriteLogAsync(LogType.Info, "查询库存信息", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    QueryStockResponseBody returnValue = JsonConvert.DeserializeObject<QueryStockResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        //save rtsorderstatus to STOCKRETURNAPPROVE_XH_JDWMS table
                        var insertresult = await sqlHelper.ExecSqlAsync($"update YW_KCK_XH_JDWMS set JDWMSSTOCK = '{returnValue.response.content.warehouseStockModelList[0].UsableNum}' where HH ='{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}'");
                        if (string.IsNullOrEmpty(insertresult))
                        {
                            await _jobLogger.WriteLogAsync(LogType.Info, "查询库存信息", $"{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}成功");
                        }
                        else
                        {
                            await _jobLogger.WriteLogAsync(LogType.Info, "查询库存信息", $"{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}失败,原因:{insertresult}");
                        }
                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "查询库存信息", $"失败返回:{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "查询库存信息", $"失败，\r\n{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 查询采购退单
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> QueryPurchaseReturn(JobPara jobPara, List<Datas> dsData)
        {
            try
            {

                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "查询采购退单", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }
                for (int i = 0; i < dsData.Count; i++)
                {
                    string billno = dsData[i].DataMain.Rows[0]["CLPSRTSNO"].SqlDataBankToString();
                    var request = new Jdwl.Api.Request.Clps.ClpsIsvRtsQueryLopRequest
                    {
                        Pin = Pin,
                        QueryRtsOrderRequest = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.QueryRtsOrderRequest
                        {
                            RtsOrderNos = billno,
                            OwnerNo = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString()
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);

                    //{"response":{"content":{"code":"1","flag":"success","message":"成功","rtsResults":[{"deliveryMode":"1","isvRtsCode":"201909090001","operatorTime":"2020-04-07 17:05:31","operatorUser":"romensfzl","ownerNo":"CBU8816093026319","receiverInfo":{"email":"数据为null","mobile":"0578-5082404","name":"宋志强测试供应商"},"rtsCode":"CBS4418046753361","rtsDetailList":[{"goodsStatus":"1","itemId":"CMG4418288906048","itemName":"布洛伪麻胶囊(得尔)","itemNo":"00000007","planOutQty":5.0,"planQty":5}],"rtsOrderStatus":"100","serialNumberList":[],"source":"9","supplierNo":"CMS4418046523757","warehouseNo":"800001573"}],"totalLine":1}, "code":0}}
                    var response = client.Execute(request);
                    await _jobLogger.WriteLogAsync(LogType.Info, "查询采购退单", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    RtsOrderQueryResponseBody returnValue = JsonConvert.DeserializeObject<RtsOrderQueryResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        //save rtsorderstatus to STOCKRETURNAPPROVE_XH_JDWMS table
                        var insertresult = await sqlHelper.ExecSqlAsync($"update STOCKRETURNAPPROVE_XH_JDWMS set RTSORDERSTATUS = '{returnValue.response.content.rtsResults[0].rtsOrderStatus }' where BILLNO ='{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}'");
                        if (string.IsNullOrEmpty(insertresult))
                        {
                            await _jobLogger.WriteLogAsync(LogType.Info, "查询采购退单", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}成功");
                        }
                        else
                        {
                            await _jobLogger.WriteLogAsync(LogType.Info, "查询采购退单", $"查询采购退单{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}失败,原因:{insertresult}");
                        }
                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "查询采购退单", $"返回:{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "查询采购退单", $"失败:{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }

        }

        /// <summary>
        /// 采购退单
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> PurchaseReturn(JobPara jobPara, List<Datas> dsData)
        {
            try
            {


                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "调拨出库信息推送", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }
                //dsData[i].DataDetail[0] 这个第i个是根据数据源中写的
                for (int i = 0; i < dsData.Count; i++)
                {
                    var RtsOrderItemList = new List<Jdwl.Api.Domain.Clps.ClpsOpenGwService.RtsOrderItem>();

                    for (int j = 0; j < dsData[i].DataDetail[0].Rows.Count; j++)
                    {
                        RtsOrderItemList.Add(new Jdwl.Api.Domain.Clps.ClpsOpenGwService.RtsOrderItem
                        {

                            ItemId = dsData[i].DataDetail[0].Rows[j]["CLPSGOODSCODE"].SqlDataBankToString(),
                            ItemQty = dsData[i].DataDetail[0].Rows[j]["QUANTITY"].SqlDataBankToInt()

                        });
                    }

                    var request = new Jdwl.Api.Request.Clps.ClpsAddRtsOrderLopRequest
                    {
                        Pin = Pin,
                        RtsOrderRequest = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.RtsOrderRequest
                        {
                            OwnerCode = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                            OutRtsOrderCode = dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString(),
                            WarehouseCode = dsData[i].DataMain.Rows[0]["warehouseNo"].SqlDataBankToString(),
                            ExtractionWay = 1,
                            RtsOrderType = 1,
                            SupplierNo = dsData[i].DataMain.Rows[0]["SUPPLIERCODE"].SqlDataBankToString(),
                            ReceiverInfo = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.ReceiverInfo
                            {
                                Mobile = dsData[i].DataMain.Rows[0]["TEL"].SqlDataBankToString(),
                                DetailAddress = dsData[i].DataMain.Rows[0]["DetailAddress"].SqlDataBankToString(),
                                City = "",
                                Company = "",
                                Area = "",
                                CountryCode = "",
                                Email = "",
                                IdNumber = "",
                                IdType = "",
                                Name = "",
                                Province = "",
                                Remark = "",
                                Tel = "",
                                Town = "",
                                ZipCode = ""
                            },
                            //出库单明细
                            RtsOrderItemList = RtsOrderItemList
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);
                    //{"response":{"content":{"clpsRtsNo":"CBS4418046753361","code":"1","flag":"success","message":"成功"}, "code":0}}
                    var response = client.Execute(request);
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送采购退单", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    RtsOrderResponseBody returnValue = JsonConvert.DeserializeObject<RtsOrderResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        if (returnValue.response.content.flag == "success")
                        {
                            var insertresult = await sqlHelper.ExecSqlAsync($"insert into STOCKRETURNAPPROVE_XH_JDWMS(BILLNO,SYNCDATE,CLPSRTSNO) values('{ dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}',{createdate},'{returnValue.response.content.clpsRtsNo}')");
                            if (string.IsNullOrEmpty(insertresult))
                            {
                                await _jobLogger.WriteLogAsync(LogType.Info, "推送采购退单", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}成功");
                            }
                            else
                            {
                                await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()},'cgtd','CODE',{createdate},'{returnValue.response.content.message}')");
                                await _jobLogger.WriteLogAsync(LogType.Info, "推送采购退单", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message + insertresult}");
                            }
                        }
                        else
                        {
                            await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()},'cgtd','CODE',{createdate},'{returnValue.response.content.message}')");
                            await _jobLogger.WriteLogAsync(LogType.Info, "推送采购退单", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message}");
                        }
                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "推送采购退单", $"失败,返回:{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "推送采购退单", $"失败,返回:{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 查询采购订单
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> QueryPurchase(JobPara jobPara, List<Datas> dsData)
        {
            try
            {

                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "查询采购订单信息", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }
                for (int i = 0; i < dsData.Count; i++)
                {
                    string billno = dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString();
                    var request = new Jdwl.Api.Request.Clps.ClpsQueryPoOrderLopRequest
                    {
                        Pin = Pin,
                        PoQueryRequest = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.PoQueryRequest
                        {
                            EntryOrderCode = billno,
                            OwnerCode = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                            WarehouseCode = dsData[i].DataMain.Rows[0]["warehouseNo"].SqlDataBankToString()
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);
                    var response = client.Execute(request);
                    //{ "response":{ "content":{ "code":"1","entryOrder":{ "clpsOrderCode":"CPL4418047893011","createTime":"2020-04-03 13:40","createUser":"romensfzl","entryOrderCode":"201709250002","ownerCode":"CBU8816093026319","poOrderStatus":"20","supplierCode":"CMS4418046522447","warehouseCode":"800001801"},"flag":"success","message":"成功","poBoxModels":[],"serialNumberList":[],"totalLines":0}, "code":0}}

                    await _jobLogger.WriteLogAsync(LogType.Info, "查询采购订单信息", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    OrderQueryResponseBody returnValue = JsonConvert.DeserializeObject<OrderQueryResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {

                        var insertresult = await sqlHelper.ExecSqlAsync($"update STOCKORDERFORM_XH_JDWMS set POORDERSTATUS ={returnValue.response.content.entryOrder.poOrderStatus},STORAGESTATUS={returnValue.response.content.entryOrder.storageStatus} where BILLNO ='{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}'");
                        if (string.IsNullOrEmpty(insertresult))
                        {
                            await _jobLogger.WriteLogAsync(LogType.Info, "查询采购订单信息", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}成功");
                        }
                        else
                        {
                            await _jobLogger.WriteLogAsync(LogType.Info, "查询采购订单信息", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}失败,原因:{insertresult}");
                        }
                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "查询采购订单信息", $"失败返回:{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "查询采购订单信息", $"失败返回:{ ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 推送采购订单
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> PurchaseOrder(JobPara jobPara, List<Datas> dsData)
        {

            if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "推送采购订单信息", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                return -1;

            }
            else
            {
                client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
            }
            //dsData[i].DataDetail[0] the i is detail what developer defined
            for (int i = 0; i < dsData.Count; i++)
            {
                try
                {
                    var OrderLines = new List<Jdwl.Api.Domain.Clps.ClpsOpenGwService.PoItemModel>();

                    for (int j = 0; j < dsData[i].DataDetail[0].Rows.Count; j++)
                    {
                        OrderLines.Add(new Jdwl.Api.Domain.Clps.ClpsOpenGwService.PoItemModel
                        {
                            OrderLineNo = dsData[i].DataDetail[0].Rows[j]["DETAILNO"].SqlDataBankToString(),
                            ItemNo = dsData[i].DataDetail[0].Rows[j]["CLPSGOODSCODE"].SqlDataBankToString(),
                            PlanQty = dsData[i].DataDetail[0].Rows[j]["QUANTITY"].SqlDataBankToInt(),
                            GoodsStatus = "1",
                            ItemPrice = dsData[i].DataDetail[0].Rows[j]["ItemPrice"].SqlDataBankToString(),
                            ItemAmount = dsData[i].DataDetail[0].Rows[j]["ItemAmount"].SqlDataBankToString(),
                            ProduceCode = dsData[i].DataDetail[0].Rows[j]["ProduceCode"].SqlDataBankToString()
                        });
                    }

                    var request = new Jdwl.Api.Request.Clps.ClpsAddPoOrderLopRequest
                    {
                        Pin = Pin,
                        PoAddRequest = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.PoAddRequest
                        {
                            EntryOrder = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.PoModel
                            {
                                EntryOrderCode = dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString(),
                                OwnerCode = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                                //京东那边的供应商编码
                                SupplierCode = dsData[i].DataMain.Rows[0]["SupplierCode"].SqlDataBankToString(),
                                WarehouseCode = dsData[i].DataMain.Rows[0]["warehouseNo"].SqlDataBankToString(),
                                OrderType = "CGRK",
                                OrderAmount = dsData[i].DataMain.Rows[0]["OrderAmount"].SqlDataBankToString()
                            },
                            OrderLines = OrderLines
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);
                    //{"response":{"content":{"code":"1","createTime":"2020-04-03 13:40:51","entryOrderCode":"CPL4418047893011","flag":"success","message":"成功"}, "code":0}}
                    var response = client.Execute(request);
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送采购订单信息", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    OrderResponseBody returnValue = JsonConvert.DeserializeObject<OrderResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        if (returnValue.response.content.flag == "success")
                        {
                            var insertresult = await sqlHelper.ExecSqlAsync($"insert into STOCKORDERFORM_XH_JDWMS(BILLNO,SYNCDATE,ENTRYORDERCODE) values('{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}',{createdate},'{returnValue.response.content.entryOrderCode}')");
                            if (string.IsNullOrEmpty(insertresult))
                            {
                                await _jobLogger.WriteLogAsync(LogType.Info, "推送采购订单信息", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}成功");
                            }
                            else
                            {
                                await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()},'cgdd','BILLNO',{createdate},'{returnValue.response.content.message}')");
                                await _jobLogger.WriteLogAsync(LogType.Info, "推送采购订单信息", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message + insertresult}");
                            }
                        }
                        else
                        {
                            await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()},'cgdd','BILLNO',{createdate},'{returnValue.response.content.message}')");
                            await _jobLogger.WriteLogAsync(LogType.Info, "推送采购订单信息", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message}");
                        }

                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "推送采购订单信息", $"失败，返回:{JsonConvert.SerializeObject(response)}");
                    }
                }
                catch (Exception ex)
                {

                    await _jobLogger.WriteLogAsync(LogType.Info, "推送采购订单信息", $"失败，返回:{ex.Message}");
                    return -1;
                }
                finally
                {
                    sqlHelper.Dispose();
                }

            }
            return 0;
        }

        /// <summary>
        /// 推送店铺信息
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> ShopInfo(JobPara jobPara, List<Datas> dsData)
        {
            try
            {


                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送店铺信息", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }
                for (int i = 0; i < dsData.Count; i++)
                {
                    var request = new Jdwl.Api.Request.Clps.ClpsSynchronizeShopLopRequest
                    {

                        Pin = Pin,
                        ShopIn = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.SynchroShopModel
                        {
                            ActionType = "add",
                            Status = "1",
                            IsvShopNo = dsData[i].DataMain.Rows[0]["CODE"].SqlDataBankToString(),
                            SpSourceNo = "1",
                            OwnerNo = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                            Type = "",
                            ShopNo = "",
                            SpShopNo = "",
                            ShopName = dsData[i].DataMain.Rows[0]["NAME"].SqlDataBankToString(),
                            Contacts = "",
                            Phone = "",
                            Address = "",
                            Email = "",
                            Fax = "",
                            AfterSaleAddress = dsData[i].DataMain.Rows[0]["ADDR"].SqlDataBankToString(),
                            AfterSaleContacts = "售后联系人",
                            AfterSalePhone = dsData[i].DataMain.Rows[0]["TEL"].SqlDataBankToString(),
                            BdOwnerNo = "",
                            OutstoreRules = "",
                            BizType = ""
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);
                    //{"response":{"content":{"code":"1","flag":"success","isvShopNo":"0001","message":"成功","shopNo":"CSP0020000045959"}, "code":0}}
                    var response = client.Execute(request);
                    // CommonHelper.Log($"入参:\r\n{JsonConvert.SerializeObject(request)}\r\n 返回数据:\r\n:{JsonConvert.SerializeObject(response)}", "推送供应商信息");
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送店铺信息", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    ShopResponseBody returnValue = JsonConvert.DeserializeObject<ShopResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        if (returnValue.response.content.flag == "success")
                        {
                            //将返回的ClpsSupplierNo保存一下
                            var insertresult = await sqlHelper.ExecSqlAsync($"insert into ORGANIZATION_XH_JDWMS(SHOPNO,CODE,SYNCDATE) values('{returnValue.response.content.shopNo}','{dsData[i].DataMain.Rows[0]["CODE"].SqlDataBankToString()}',{createdate})");
                            if (string.IsNullOrEmpty(insertresult))
                            {
                                await _jobLogger.WriteLogAsync(LogType.Info, "推送店铺信息", $"{dsData[i].DataMain.Rows[0]["CODE"].SqlDataBankToString()}成功");
                            }
                            else
                            {
                                await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["CODE"].SqlDataBankToString()},'dp','CODE',{createdate},'{returnValue.response.content.message}')");
                                await _jobLogger.WriteLogAsync(LogType.Info, "推送店铺信息", $"{dsData[i].DataMain.Rows[0]["CODE"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message + insertresult}");
                            }
                        }
                        else
                        {
                            await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["CODE"].SqlDataBankToString()},'dp','CODE',{createdate},'{returnValue.response.content.message}')");
                            await _jobLogger.WriteLogAsync(LogType.Info, "推送店铺信息", $"{dsData[i].DataMain.Rows[0]["CODE"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message}");
                        }
                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "推送店铺信息", $"失败返回:{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "推送店铺信息", $"失败，{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 订单关闭
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> OrderClose(JobPara jobPara, List<Datas> dsData)
        {
            try
            {
                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "异常订单关闭", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }
                for (int i = 0; i < dsData.Count; i++)
                {
                    var request = new Jdwl.Api.Request.Clps.ClpsClpsOrderCancelLopRequest
                    {
                        Pin = Pin,
                        OrderCancelRequest = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.OrderCancelRequest
                        {
                            WarehouseCode = dsData[i].DataMain.Rows[0]["warehouseNo"].SqlDataBankToString(),
                            OwnerCode = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                            OrderId = dsData[i].DataMain.Rows[0]["ORDERID"].SqlDataBankToString(),
                            OrderType = dsData[i].DataMain.Rows[0]["ORDERTYPE"].SqlDataBankToString()
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);
                    //{"response":{"content":{"code":"1","createTime":"2020-04-09 14:11:02","entryOrderCode":"CPL4418047912171","flag":"success","message":"成功"}, "code":0}}
                    var response = client.Execute(request);
                    await _jobLogger.WriteLogAsync(LogType.Info, "异常订单关闭", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    OrderResponseBody returnValue = JsonConvert.DeserializeObject<OrderResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        if (returnValue.response.content.flag == "success")
                        {
                            var insertresult = await sqlHelper.ExecSqlAsync($"insert into CloseOrder_XH_JDWMS(BILLNO,SYNCDATE,CLPSCODE,TYPE) values('{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}',{createdate},'{ dsData[i].DataMain.Rows[0]["ORDERID"].SqlDataBankToString()}','{dsData[i].DataMain.Rows[0]["ORDERTYPE"].SqlDataBankToString()}')");
                            if (string.IsNullOrEmpty(insertresult))
                            {
                                await _jobLogger.WriteLogAsync(LogType.Info, "异常订单关闭", $"{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()}成功");
                            }
                            else
                            {
                                await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()},'djqx','BILLNO',{createdate},'{returnValue.response.content.message}')");
                                await _jobLogger.WriteLogAsync(LogType.Info, "异常订单关闭", $"失败,原因:{returnValue.response.content.message + insertresult}");
                            }
                        }
                        else
                        {
                            await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["BILLNO"].SqlDataBankToString()},'djqx','BILLNO',{createdate},'{returnValue.response.content.message}')");
                            await _jobLogger.WriteLogAsync(LogType.Info, "异常订单关闭", $"失败,原因:{returnValue.response.content.message}");
                        }

                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "异常订单关闭", $"失败,返回:{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "异常订单关闭", $"失败,返回:{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 推送商品信息到笛佛
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> WdgjGoods(JobPara jobPara, List<Datas> dsData)
        {
            try
            {
                if (string.IsNullOrEmpty(wdgj_appkey) || string.IsNullOrEmpty(wdgj_appsecret) || string.IsNullOrEmpty(wdgj_accesstoken))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息到笛佛", $"失败:数据源中缺少wdgj_appkey、wdgj_appsecret、wdgj_accesstoken!");
                    return -1;

                }
                OpenApi wdgjOpenApi = new OpenApi
                {
                    Appkey = wdgj_appkey,
                    AppSecret = wdgj_appsecret,
                    AccessToken = wdgj_accesstoken,
                    Timestamp = ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000).ToString(),
                    Method = "wdgj.goods.create",
                    Format = "json",
                    Versions = "1.0",
                };


                ArrayList arrayList = new ArrayList();
                List<GoodsDatainfo> datainfo = new List<GoodsDatainfo>();
                for (int i = 0; i < dsData.Count; i++)
                {
                    datainfo.Add(new GoodsDatainfo
                    {
                        goodsno = dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString(),
                        goodsname = dsData[i].DataMain.Rows[0]["PM"].SqlDataBankToString(),
                        unit = dsData[i].DataMain.Rows[0]["PDW"].SqlDataBankToString(),
                        bgift = dsData[i].DataMain.Rows[0]["ISGIFT"].SqlDataBankToInt(),
                        bmultispec = "0",
                        bnegativestock = "0",
                        barcode = dsData[i].DataMain.Rows[0]["TM"].SqlDataBankToString(),
                        speclist = new List<specinfo>
                            {
                                new specinfo
                                {
                                    bblockup = "0",
                                    bfixcost = "0"
                                }
                            }
                    });
                    arrayList.Add(dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString());
                }

                WdgjGoodsModel wdgjGoodsModel = new WdgjGoodsModel
                {
                    datalist = datainfo
                };

                string createGoodsJson = JsonConvert.SerializeObject(wdgjGoodsModel, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                wdgjOpenApi.AppParam.content = createGoodsJson;
                string postresult = wdgjOpenApi.HttpPostString();
                WdgjGoodsResponse wdgjGoodsResponse = JsonConvert.DeserializeObject<WdgjGoodsResponse>(postresult);
                await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息到笛佛", $"入参：\r\n{createGoodsJson},返回Json:\r\n{postresult}");
                if (wdgjGoodsResponse.datalist?.Count > 0)
                {
                    string falsehhs = "";
                    for (int i = 0; i < wdgjGoodsResponse.datalist.Count; i++)
                    {
                        if (falsehhs == "")
                        {
                            falsehhs = $"'{wdgjGoodsResponse.datalist[i].goodsno}'";
                        }
                        else
                        {
                            falsehhs += $",'{wdgjGoodsResponse.datalist[i].goodsno}'";
                        }
                        arrayList.Remove(wdgjGoodsResponse.datalist[i].goodsno);
                    }
                    string falseupdate = $"update yw_kck_xh_jdwms set ISWDGJ = '2' where hh in ({falsehhs})";
                    var updateresult = await sqlHelper.ExecSqlAsync(falseupdate);
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息到笛佛", $"更新sql语句:{falseupdate}\r\n,返回信息:{updateresult}");
                }

                if (wdgjGoodsResponse.returncode == "0")
                {
                    string successhhs = "";
                    for (int i = 0; i < arrayList.Count; i++)
                    {
                        if (successhhs == "")
                        {
                            successhhs = $"'{arrayList[i]}'";
                        }
                        else
                        {
                            successhhs += $",'{arrayList[i]}'";
                        }
                    }
                    string successupdate = $"update yw_kck_xh_jdwms set ISWDGJ = '1' where hh in ({successhhs})";
                    var updateresult = await sqlHelper.ExecSqlAsync(successupdate);
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息到笛佛", $"更新sql语句:{successupdate},返回信息:{updateresult}");
                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息到笛佛", $"失败:{ ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 笛佛供应商信息
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> WdgjSupplier(JobPara jobPara, List<Datas> dsData)
        {
            try
            {
                if (string.IsNullOrEmpty(wdgj_appkey) || string.IsNullOrEmpty(wdgj_appsecret) || string.IsNullOrEmpty(wdgj_accesstoken))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息到笛佛", $"失败:数据源中缺少wdgj_appkey、wdgj_appsecret、wdgj_accesstoken!");
                    return -1;

                }
                OpenApi wdgjOpenApi = new OpenApi
                {
                    Appkey = wdgj_appkey,
                    AppSecret = wdgj_appsecret,
                    AccessToken = wdgj_accesstoken,
                    Timestamp = ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000).ToString(),
                    Method = "wdgj.prover.create",
                    Format = "json",
                    Versions = "1.0"

                };
                ArrayList arrayList = new ArrayList();
                List<SupplierDatainfo> supplierDatainfos = new List<SupplierDatainfo>();
                for (int i = 0; i < dsData.Count; i++)
                {
                    supplierDatainfos.Add(new SupplierDatainfo
                    {
                        providername = dsData[i].DataMain.Rows[0]["MC"].SqlDataBankToString(),
                        providerno = dsData[i].DataMain.Rows[0]["TJBH"].SqlDataBankToString(),
                        provideralias = dsData[i].DataMain.Rows[0]["MC"].SqlDataBankToString()
                    });

                    arrayList.Add(dsData[i].DataMain.Rows[0]["TJBH"].SqlDataBankToString());
                }

                WdgjSupplierModel wdgjSupplier = new WdgjSupplierModel
                {
                    datalist = supplierDatainfos
                };
                string createSuppliersJson = JsonConvert.SerializeObject(wdgjSupplier, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                wdgjOpenApi.AppParam.content = createSuppliersJson;

                string postresult = wdgjOpenApi.HttpPostString();
                WdgjSupplierResponse supplierResponse = JsonConvert.DeserializeObject<WdgjSupplierResponse>(postresult);
                await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息到笛佛", $"入参：\r\n{createSuppliersJson},返回Json:\r\n{postresult}");
                if (supplierResponse.datalist?.Count > 0)
                {
                    string falsehhs = "";
                    for (int i = 0; i < supplierResponse.datalist.Count; i++)
                    {
                        if (falsehhs == "")
                        {
                            falsehhs = $"'{supplierResponse.datalist[i].providerno}'";
                        }
                        else
                        {
                            falsehhs += $",'{supplierResponse.datalist[i].providerno}'";
                        }
                        // remove the error hh
                        arrayList.Remove(supplierResponse.datalist[i].providerno);
                    }

                    string falseupdate = $"update GL_SUPER_XH_JDWMS set ISWDGJ = '2' where TJBH in({falsehhs})";
                    var updateresult = await sqlHelper.ExecSqlAsync(falseupdate);
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息到笛佛", $"更新sql语句:{falseupdate}\r\n,返回信息:{updateresult}");
                }

                if (supplierResponse.returncode == "0")
                {
                    string successhhs = "";
                    for (int i = 0; i < arrayList.Count; i++)
                    {
                        if (successhhs == "")
                        {
                            successhhs = $"'{arrayList[i]}'";
                        }
                        else
                        {
                            successhhs += $",'{arrayList[i]}'";
                        }
                    }
                    string successupdate = $"update GL_SUPER_XH_JDWMS set ISWDGJ = '1' where TJBH in({successhhs})";
                    var updateresult = await sqlHelper.ExecSqlAsync(successupdate);
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息到笛佛", $"更新sql语句:{successupdate},返回信息:{updateresult}");
                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息到笛佛", $"失败:{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 推送供应商信息
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> SupplierInfo(JobPara jobPara, List<Datas> dsData)
        {
            try
            {
                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }


                for (int i = 0; i < dsData.Count; i++)
                {
                    var request = new Jdwl.Api.Request.Clps.ClpsSynchronizeSupplierLopRequest
                    {
                        Pin = Pin,
                        SupplierModel = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.SynchroSupplierModel
                        {
                            ActionType = "add",
                            Status = "2",
                            SupplierName = dsData[i].DataMain.Rows[0]["MC"].SqlDataBankToString(),
                            SupplierType = "",
                            ClpsSupplierNo = "",
                            IsvSupplierNo = dsData[i].DataMain.Rows[0]["TJBH"].SqlDataBankToString(),
                            Address = dsData[i].DataMain.Rows[0]["ADDR"].SqlDataBankToString(),
                            City = "",
                            Contacts = dsData[i].DataMain.Rows[0]["LXR"].SqlDataBankToString(),
                            County = "",
                            Email = dsData[i].DataMain.Rows[0]["EMAIL"].SqlDataBankToString(),
                            Ext1 = "",
                            Ext2 = "",
                            Ext3 = "",
                            Ext4 = "",
                            Ext5 = "",
                            Fax = "",
                            Mobile = dsData[i].DataMain.Rows[0]["TEL"].SqlDataBankToString(),
                            OwnerName = dsData[i].DataMain.Rows[0]["OwnerName"].SqlDataBankToString(),
                            OwnerNo = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                            Phone = dsData[i].DataMain.Rows[0]["TEL"].SqlDataBankToString(),
                            Province = "",
                            Town = ""
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);
                    //{"response":{"content":{"clpsSupplierNo":"CMS4418046522447","code":"1","flag":"success","isvSupplierNo":"00002","message":"成功"}, "code":0}}
                    var response = client.Execute(request);
                    // CommonHelper.Log($"入参:\r\n{JsonConvert.SerializeObject(request)}\r\n 返回数据:\r\n:{JsonConvert.SerializeObject(response)}", "推送供应商信息");
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    SupplierResponseBody returnValue = JsonConvert.DeserializeObject<SupplierResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        if (returnValue.response.content.flag == "success")
                        {
                            //将返回的ClpsSupplierNo保存一下
                            var insertresult = await sqlHelper.ExecSqlAsync($"insert into GL_SUPER_XH_JDWMS(CLPSSUPPLIERNO,TJBH,SYNCDATE) values('{returnValue.response.content.clpsSupplierNo}','{dsData[i].DataMain.Rows[0]["TJBH"].SqlDataBankToString()}',{createdate})");
                            if (string.IsNullOrEmpty(insertresult))
                            {
                                await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息", $"供应商信息{dsData[i].DataMain.Rows[0]["TJBH"].SqlDataBankToString()}成功");
                            }
                            else
                            {
                                await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["TJBH"].SqlDataBankToString()},'gys','TJBH',{createdate},'{returnValue.response.content.message}')");
                                await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息", $"{dsData[i].DataMain.Rows[0]["TJBH"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message + insertresult}");
                            }
                        }
                        else
                        {
                            await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["TJBH"].SqlDataBankToString()},'gys','TJBH',{createdate},'{returnValue.response.content.message}')");
                            await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息", $"{dsData[i].DataMain.Rows[0]["TJBH"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message}");
                        }
                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息", $"失败:{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "推送供应商信息", $"失败:{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 推送商品信息
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> DrugInfo(JobPara jobPara, List<Datas> dsData)
        {
            try
            {


                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }
                for (int i = 0; i < dsData.Count; i++)
                {
                    var OutPackUoms = new List<Jdwl.Api.Domain.Clps.ClpsOpenGwService.OutPackUom>();
                    OutPackUoms.Add(new Jdwl.Api.Domain.Clps.ClpsOpenGwService.OutPackUom
                    {
                        Height = "",
                        Length = "",
                        NetWeight = "",
                        OutUomName = "BigPack",
                        OutUomNo = "LARGEPACKUNIT",
                        OutUomQty = 1,
                        Volume = "",
                        Width = ""
                    });

                    var PackRules = new List<Jdwl.Api.Domain.Clps.ClpsOpenGwService.PackRule>();
                    PackRules.Add(new Jdwl.Api.Domain.Clps.ClpsOpenGwService.PackRule
                    {
                        OutPackUoms = OutPackUoms,
                        PackId = "BigPack",
                        PackName = "BigPack"
                    });

                    var request = new Jdwl.Api.Request.Clps.ClpsTransportSingleItemLopRequest
                    {
                        Pin = Pin,
                        SingleItemRequest = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.SingleItemRequest
                        {
                            ActionType = dsData[i].DataMain.Rows[0]["actionType"].SqlDataBankToString(),
                            WarehouseCode = dsData[i].DataMain.Rows[0]["warehouseNo"].SqlDataBankToString(),
                            OwnerCode = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                            Item = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.Item
                            {
                                ItemCode = dsData[i].DataMain.Rows[0].DataRowGetStringValue("HH"),
                                ItemId = dsData[i].DataMain.Rows[0].DataRowGetStringValue("actionType") == "add" ? "" : dsData[i].DataMain.Rows[0].DataRowGetStringValue("ItemId"),
                                GoodsNumFloat = 0,
                                ShopNos = dsData[i].DataMain.Rows[0].DataRowGetStringValue("shopNos"),
                                SupplierCode = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SupplierCode"),
                                SupplierName = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SupplierName"),
                                GoodsCode = dsData[i].DataMain.Rows[0].DataRowGetStringValue("GoodsCode"),
                                ClpsGoodsCode = dsData[i].DataMain.Rows[0].DataRowGetStringValue("ClpsGoodsCode"),
                                ItemName = dsData[i].DataMain.Rows[0].DataRowGetStringValue("PM"),
                                ShortName = dsData[i].DataMain.Rows[0].DataRowGetStringValue("BM"),
                                EnglishName = dsData[i].DataMain.Rows[0].DataRowGetStringValue("EnglishName"),
                                BarCode = dsData[i].DataMain.Rows[0].DataRowGetStringValue("TM"),
                                SkuProperty = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SkuProperty"),
                                StockUnitCode = dsData[i].DataMain.Rows[0].DataRowGetStringValue("PDW"),
                                StockUnitDesc = dsData[i].DataMain.Rows[0].DataRowGetStringValue("PDW"),
                                Length = dsData[i].DataMain.Rows[0].DataRowGetIntValue("Length"),
                                Width = dsData[i].DataMain.Rows[0].DataRowGetIntValue("Width"),
                                Height = dsData[i].DataMain.Rows[0].DataRowGetIntValue("Height"),
                                Volume = dsData[i].DataMain.Rows[0].DataRowGetIntValue("Volume"),
                                GrossWeight = dsData[i].DataMain.Rows[0].DataRowGetIntValue("GrossWeight"),
                                NetWeight = dsData[i].DataMain.Rows[0].DataRowGetIntValue("NetWeight"),
                                Color = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Color"),
                                Size = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Size"),
                                Title = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Title"),
                                CategoryId = dsData[i].DataMain.Rows[0].DataRowGetStringValue("CategoryId"),
                                CategoryName = dsData[i].DataMain.Rows[0].DataRowGetStringValue("CategoryName"),
                                PricingCategory = dsData[i].DataMain.Rows[0].DataRowGetStringValue("PricingCategory"),
                                SafetyStock = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SafetyStock"),
                                ItemType = dsData[i].DataMain.Rows[0].DataRowGetStringValue("ItemType") == "" ? "ZC" : dsData[i].DataMain.Rows[0].DataRowGetStringValue("ItemType"),
                                TagPrice = dsData[i].DataMain.Rows[0].DataRowGetIntValue("TagPrice"),
                                RetailPrice = dsData[i].DataMain.Rows[0].DataRowGetIntValue("RetailPrice"),
                                CostPrice = dsData[i].DataMain.Rows[0].DataRowGetIntValue("CostPrice"),
                                PurchasePrice = dsData[i].DataMain.Rows[0].DataRowGetIntValue("PurchasePrice"),
                                SeasonCode = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SeasonCode"),
                                SeasonName = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SeasonName"),
                                BrandCode = dsData[i].DataMain.Rows[0].DataRowGetStringValue("BrandCode"),
                                BrandName = dsData[i].DataMain.Rows[0].DataRowGetStringValue("BrandName"),
                                IsSNMgmt = dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsSNMgmt") == "" ? "N" : dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsSNMgmt"),
                                ProductDate = dsData[i].DataMain.Rows[0].DataRowGetStringValue("ProductDate"),
                                ExpireDate = dsData[i].DataMain.Rows[0].DataRowGetStringValue("ExpireDate"),
                                IsShelfLifeMgmt = dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsShelfLifeMgmt"),
                                ShelfLife = dsData[i].DataMain.Rows[0].DataRowGetIntValue("ShelfLife"),
                                RejectLifecycle = dsData[i].DataMain.Rows[0].DataRowGetIntValue("RejectLifecycle"),
                                LockupLifecycle = dsData[i].DataMain.Rows[0].DataRowGetIntValue("LockupLifecycle"),
                                AdventLifecycle = dsData[i].DataMain.Rows[0].DataRowGetIntValue("AdventLifecycle"),
                                IsBatchMgmt = dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsBatchMgmt") == "" ? "Y" : dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsBatchMgmt"),
                                BatchCode = dsData[i].DataMain.Rows[0].DataRowGetStringValue("BatchCode"),
                                BatchRemark = dsData[i].DataMain.Rows[0].DataRowGetStringValue("BatchRemark"),
                                OriginAddress = dsData[i].DataMain.Rows[0].DataRowGetStringValue("OriginAddress"),
                                ApprovalNumber = dsData[i].DataMain.Rows[0].DataRowGetStringValue("ApprovalNumber"),
                                IsFragile = dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsFragile"),
                                IsHazardous = dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsHazardous"),
                                Remark = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Remark"),
                                CreateTime = dsData[i].DataMain.Rows[0].DataRowGetStringValue("CreateTime"),
                                UpdateTime = dsData[i].DataMain.Rows[0].DataRowGetStringValue("UpdateTime"),
                                IsValid = dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsValid") == "" ? "Y" : dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsValid"),
                                IsSku = dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsSku"),
                                PackageMaterial = dsData[i].DataMain.Rows[0].DataRowGetStringValue("PackageMaterial"),
                                SellerGoodsSign = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SellerGoodsSign"),
                                SpGoodsNo = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SpGoodsNo"),
                                InstoreThreshold = dsData[i].DataMain.Rows[0].DataRowGetIntValue("InstoreThreshold"),
                                OutstoreThreshold = dsData[i].DataMain.Rows[0].DataRowGetIntValue("OutstoreThreshold"),
                                Manufacturer = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SCDW"),
                                SizeDefinition = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SizeDefinition"),
                                CheapGift = dsData[i].DataMain.Rows[0].DataRowGetIntValue("ISGIFT") == 0 ? "N" : "Y",
                                Quality = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Quality"),
                                Expensive = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Expensive"),
                                Luxury = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Luxury"),
                                Liquid = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Liquid"),
                                Consumables = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Consumables"),
                                Abnormal = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Abnormal"),
                                Imported = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Imported"),
                                Health = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Health"),
                                Temperature = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Temperature"),
                                TemperatureCeil = dsData[i].DataMain.Rows[0].DataRowGetStringValue("TemperatureCeil"),
                                TemperatureFloor = dsData[i].DataMain.Rows[0].DataRowGetStringValue("TemperatureFloor"),
                                Humidity = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Humidity"),
                                HumidityCeil = dsData[i].DataMain.Rows[0].DataRowGetStringValue("HumidityCeil"),
                                HumidityFloor = dsData[i].DataMain.Rows[0].DataRowGetStringValue("HumidityFloor"),
                                Movable = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Movable"),
                                Service3g = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Service3g"),
                                Sample = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Sample"),
                                Odor = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Odor"),
                                Sex = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Sex"),
                                Precious = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Precious"),
                                MixedBatch = dsData[i].DataMain.Rows[0].DataRowGetStringValue("MixedBatch"),
                                FashionNo = dsData[i].DataMain.Rows[0].DataRowGetStringValue("FashionNo"),
                                CustomMade = dsData[i].DataMain.Rows[0].DataRowGetStringValue("CustomMade"),
                                AirMark = dsData[i].DataMain.Rows[0].DataRowGetStringValue("AirMark"),
                                LossRate = dsData[i].DataMain.Rows[0].DataRowGetStringValue("LossRate"),
                                SellPeriod = dsData[i].DataMain.Rows[0].DataRowGetStringValue("SellPeriod"),
                                IsPMX = dsData[i].DataMain.Rows[0].DataRowGetStringValue("IsPMX"),
                                QualityCheckRate = dsData[i].DataMain.Rows[0].DataRowGetStringValue("QualityCheckRate"),
                                ProductSeason = dsData[i].DataMain.Rows[0].DataRowGetStringValue("ProductSeason"),
                                MaterialNo = dsData[i].DataMain.Rows[0].DataRowGetStringValue("MaterialNo"),
                                PrIntegerProductId = dsData[i].DataMain.Rows[0].DataRowGetStringValue("PrIntegerProductId"),
                                PrIntegerName = dsData[i].DataMain.Rows[0].DataRowGetStringValue("PrIntegerName"),
                                BundleFlag = dsData[i].DataMain.Rows[0].DataRowGetStringValue("BundleFlag"),
                                ProductCategory = dsData[i].DataMain.Rows[0].DataRowGetIntValue("ISGIFT"),
                                Category = dsData[i].DataMain.Rows[0].DataRowGetStringValue("YPBZ") == "1" ? "Y" : "N",
                                Storage = dsData[i].DataMain.Rows[0].DataRowGetStringValue("CCTJ") == "" ? "常温" : dsData[i].DataMain.Rows[0].DataRowGetStringValue("CCTJ"),
                                Type = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Type"),
                                Specification = dsData[i].DataMain.Rows[0].DataRowGetStringValue("GG"),
                                GenericName = dsData[i].DataMain.Rows[0].DataRowGetStringValue("GenericName"),
                                Dosage = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Dosage"),
                                UseMethods = dsData[i].DataMain.Rows[0].DataRowGetStringValue("UseMethods"),
                                PackingUnit = dsData[i].DataMain.Rows[0].DataRowGetStringValue("PackingUnit"),
                                Efficacy = dsData[i].DataMain.Rows[0].DataRowGetStringValue("Efficacy"),
                                RegistrationNum = dsData[i].DataMain.Rows[0].DataRowGetStringValue("RegistrationNum"),
                                ApprovalNum = dsData[i].DataMain.Rows[0].DataRowGetStringValue("ApprovalNum"),
                                CuringType = dsData[i].DataMain.Rows[0].DataRowGetIntValue("CuringType"),
                                CuringPeriod = dsData[i].DataMain.Rows[0].DataRowGetIntValue("CuringPeriod"),
                                WarmLayer = dsData[i].DataMain.Rows[0].DataRowGetStringValue("WarmLayer"),
                                QualifyTypes = dsData[i].DataMain.Rows[0].DataRowGetIntValue("QualifyTypes"),
                                DoseType = dsData[i].DataMain.Rows[0].DataRowGetIntValue("DoseType"),
                                PackRules = PackRules,
                                Serial = 0,
                                TraceNoCollect = 0

                            }
                        }
                    };

                    var jsonrequest = JsonConvert.SerializeObject(request);
                    var response = client.Execute(request);
                    await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //{"response":{"content":{"clpsGoodsCode":"CMG4418274978871","code":"1","flag":"success","itemCode":"00000002","message":"商品同步成功"}, "code":0}}
                    //如果请求执行正确,从这里获取强类型返回值
                    SingelResponseBody returnValue = JsonConvert.DeserializeObject<SingelResponseBody>(response.Body);
                    if (returnValue.response.code == 0)
                    {
                        if (returnValue.response.content.flag == "success")
                        {
                            if (dsData[i].DataMain.Rows[0]["actionType"].SqlDataBankToString() == "add")
                            {
                                string insertsql = $"insert into YW_KCK_XH_JDWMS(CLPSGOODSCODE,HH,SYNCDATE) values('{returnValue.response.content.clpsGoodsCode}','{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}',{createdate})";
                                var insertresult = await sqlHelper.ExecSqlAsync(insertsql);
                                if (string.IsNullOrEmpty(insertresult))
                                {
                                    await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息", $"{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}成功");
                                }
                                else
                                {
                                    await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()},'sp','HH',{createdate},'{returnValue.response.content.message}')");
                                    await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息", $"{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message + insertresult}");
                                }
                            }
                        }
                        else
                        {
                            await sqlHelper.ExecSqlAsync($"insert into ERRORDATA_XH_JDWMS(GUID,ERRORNO,ERRORTYPE,MAINFIELD,ERRORDATE,ERRORMSG) values('{Guid.NewGuid():N}',{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()},'sp','HH',{createdate},'{returnValue.response.content.message}')");
                            await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息", $"{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}失败,原因:{returnValue.response.content.message}");
                        }
                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息", $"失败返回:{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "推送商品信息", $"失败:{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 查询商品信息
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> QueryDrug(JobPara jobPara, List<Datas> dsData)
        {
            try
            {

                if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(Pin))
                {
                    await _jobLogger.WriteLogAsync(LogType.Info, "查询商品信息", $"失败:数据源中缺少serverUrl、accessToken、appKey、appSecret、Pin!");
                    return -1;

                }
                else
                {
                    client = new DefaultJdClient(serverUrl, accessToken, appKey, appSecret);
                }
                for (int i = 0; i < dsData.Count; i++)
                {
                    var request = new Jdwl.Api.Request.Clps.ClpsQueryItemLopRequest
                    {
                        Pin = Pin,
                        ItemQueryRequest = new Jdwl.Api.Domain.Clps.ClpsOpenGwService.ItemQueryRequest
                        {
                            Page = 1,
                            PageSize = 10,
                            ItemCode = dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString(),
                            OwnerCode = dsData[i].DataMain.Rows[0]["ownerCode"].SqlDataBankToString(),
                            QueryType = "2"
                        }
                    };
                    var jsonrequest = JsonConvert.SerializeObject(request);
                    var response = client.Execute(request);
                    await _jobLogger.WriteLogAsync(LogType.Info, "查询商品信息", $"入参：\r\n{jsonrequest},返回Json:\r\n{response.Body}");
                    //如果请求执行正确,从这里获取强类型返回值
                    SelectGoods returnValue = JsonConvert.DeserializeObject<SelectGoods>(response.Body);
                    if (response.Code == 0)
                    {
                        var insertresult = await sqlHelper.ExecSqlAsync($"update yw_kck_xh_jdwms set CLPSGOODSCODE ='{returnValue.response.content.items[0].itemId}' where HH ='{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}'");
                        if (string.IsNullOrEmpty(insertresult))
                        {
                            await _jobLogger.WriteLogAsync(LogType.Info, "查询商品信息", $"{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}成功");
                        }
                        else
                        {
                            await _jobLogger.WriteLogAsync(LogType.Info, "查询商品信息", $"{dsData[i].DataMain.Rows[0]["HH"].SqlDataBankToString()}失败,原因:{insertresult}");
                        }
                    }
                    //响应的原始报文,如果请求失败,从这里获取错误消息代码
                    else
                    {
                        await _jobLogger.WriteLogAsync(LogType.Info, "查询商品信息", $"返回:{JsonConvert.SerializeObject(response)}");
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                await _jobLogger.WriteLogAsync(LogType.Info, "查询商品信息", $"失败，\r\n{ex.Message}");
                return -1;
            }
            finally
            {
                sqlHelper.Dispose();
            }
        }

        /// <summary>
        /// 重置错误数据
        /// </summary>
        /// <param name="jobPara"></param>
        /// <param name="dsData"></param>
        /// <returns></returns>
        public async Task<int> ResetError(JobPara jobPara, List<Datas> dsData)
        {
            await sqlHelper.ExecSqlAsync("delete ERRORDATA_XH_JDWMS");
            await _jobLogger.WriteLogAsync(LogType.Info, "错误的数据已重置", $"ERRORDATA_XH_JDWMS表已清空，推送错误的数据已重置");
            return 0;
        }
    }
}


//('09017399','09017401','07010289','07020580','09017174','1002-0','09015432','00000013','09017400','07010290','09015392','09015437','09017177','09015837','09015839','09015394','09015439','09017176','09015431','00000001','00000336','09017402','07010288','09015391','09017175','09015395')
