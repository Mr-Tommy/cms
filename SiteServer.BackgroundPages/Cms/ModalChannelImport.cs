﻿using System;
using System.Collections.Specialized;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.CMS.Core;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.ImportExport;
using SiteServer.Utils.Enumerations;
using SiteServer.BackgroundPages.Core;

namespace SiteServer.BackgroundPages.Cms
{
    public class ModalChannelImport : BasePageCms
    {
        protected DropDownList DdlParentChannelId;
        public HtmlInputFile HifFile;
        public DropDownList DdlIsOverride;

        private bool[] _isLastNodeArray;

        public static string GetOpenWindowString(int siteId, int channelId)
        {
            return LayerUtils.GetOpenScript("导入栏目",
                PageUtilsEx.GetCmsUrl(siteId, nameof(ModalChannelImport), new NameValueCollection
                {
                    {"channelId", channelId.ToString()}
                }), 600, 300);
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            if (IsPostBack) return;

            var channelId = AuthRequest.GetQueryInt("channelId", SiteId);
            var channelIdList = ChannelManager.GetChannelIdList(SiteId);
            var nodeCount = channelIdList.Count;
            _isLastNodeArray = new bool[nodeCount];
            foreach (var theChannelId in channelIdList)
            {
                var channelInfo = ChannelManager.GetChannelInfo(SiteId, theChannelId);
                var itemChannelId = channelInfo.Id;
                var nodeName = channelInfo.ChannelName;
                var parentsCount = channelInfo.ParentsCount;
                var isLastNode = channelInfo.LastNode;
                var value = IsOwningChannelId(itemChannelId) ? itemChannelId.ToString() : string.Empty;
                value = (channelInfo.IsChannelAddable) ? value : string.Empty;
                if (!string.IsNullOrEmpty(value))
                {
                    if (!HasChannelPermissions(theChannelId, ConfigManager.ChannelPermissions.ChannelAdd))
                    {
                        value = string.Empty;
                    }
                }
                var listitem = new ListItem(GetTitle(itemChannelId, nodeName, parentsCount, isLastNode), value);
                if (itemChannelId == channelId)
                {
                    listitem.Selected = true;
                }
                DdlParentChannelId.Items.Add(listitem);
            }
        }

        public string GetTitle(int channelId, string nodeName, int parentsCount, bool isLastNode)
        {
            var str = "";
            if (channelId == SiteId)
            {
                isLastNode = true;
            }
            if (isLastNode == false)
            {
                _isLastNodeArray[parentsCount] = false;
            }
            else
            {
                _isLastNodeArray[parentsCount] = true;
            }
            for (var i = 0; i < parentsCount; i++)
            {
                str = string.Concat(str, _isLastNodeArray[i] ? "　" : "│");
            }
            str = string.Concat(str, isLastNode ? "└" : "├");
            str = string.Concat(str, nodeName);
            return str;
        }

        public override void Submit_OnClick(object sender, EventArgs e)
        {
            if (HifFile.PostedFile != null && "" != HifFile.PostedFile.FileName)
            {
                var filePath = HifFile.PostedFile.FileName;
                if (!EFileSystemTypeUtils.IsZip(PathUtils.GetExtension(filePath)))
                {
                    FailMessage("必须上传Zip压缩文件");
                    return;
                }

                try
                {
                    var localFilePath = PathUtils.GetTemporaryFilesPath(PathUtils.GetFileName(filePath));

                    HifFile.PostedFile.SaveAs(localFilePath);

                    var importObject = new ImportObject(SiteId, AuthRequest.AdminName);
                    importObject.ImportChannelsAndContentsByZipFile(TranslateUtils.ToInt(DdlParentChannelId.SelectedValue), localFilePath, TranslateUtils.ToBool(DdlIsOverride.SelectedValue));

                    AuthRequest.AddSiteLog(SiteId, "导入栏目");

                    LayerUtils.Close(Page);
                }
                catch (Exception ex)
                {
                    FailMessage(ex, "导入栏目失败！");
                }
            }
        }
    }
}
