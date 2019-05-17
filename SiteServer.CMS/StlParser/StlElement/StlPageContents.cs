﻿using System;
using System.Text;
using SiteServer.CMS.Api.Sys.Stl;
using SiteServer.CMS.Core;
using SiteServer.CMS.DataCache.Stl;
using SiteServer.CMS.Model;
using SiteServer.CMS.StlParser.Model;
using SiteServer.CMS.StlParser.Utility;
using SiteServer.Utils.Enumerations;

namespace SiteServer.CMS.StlParser.StlElement
{
    [StlElement(Title = "翻页内容列表", Description = "通过 stl:pageContents 标签在模板中显示翻页内容列表")]
    public class StlPageContents : StlContents
    {
        public new const string ElementName = "stl:pageContents";

        [StlAttribute(Title = "每页显示的内容数目")]
        public const string PageNum = nameof(PageNum);

        [StlAttribute(Title = "翻页中生成的静态页面最大数，剩余页面将动态获取")]
        public const string MaxPage = nameof(MaxPage);

        private readonly string _stlPageContentsElement;

        private readonly string _sqlString;
        private readonly PageInfo _pageInfo;
        private readonly ContextInfo _contextInfo;
        private readonly ListInfo _listInfo;

        public StlPageContents(string stlPageContentsElement, PageInfo pageInfo, ContextInfo contextInfo)
        {
            _stlPageContentsElement = stlPageContentsElement;
            _pageInfo = pageInfo;
            _contextInfo = contextInfo;

            var stlElementInfo = StlParserUtility.ParseStlElement(stlPageContentsElement);

            _contextInfo = contextInfo.Clone(stlPageContentsElement, stlElementInfo.InnerHtml, stlElementInfo.Attributes);

            _listInfo = ListInfo.GetListInfo(_pageInfo, _contextInfo, EContextType.Content);

            var channelId = StlDataUtility.GetChannelIdByLevel(_pageInfo.SiteId, _contextInfo.ChannelId, _listInfo.UpLevel, _listInfo.TopLevel);

            channelId = StlDataUtility.GetChannelIdByChannelIdOrChannelIndexOrChannelName(_pageInfo.SiteId, channelId, _listInfo.ChannelIndex, _listInfo.ChannelName);

            _sqlString = StlDataUtility.GetStlPageContentsSqlString(_pageInfo.SiteInfo, channelId, _listInfo);
        }

        //API StlActionsSearchController调用
        public StlPageContents(string stlPageContentsElement, PageInfo pageInfo, ContextInfo contextInfo, int pageNum, ChannelInfo channelInfo, string whereString)
        {
            _pageInfo = pageInfo;
            _contextInfo = contextInfo;

            var stlElementInfo = StlParserUtility.ParseStlElement(stlPageContentsElement);
            _contextInfo = contextInfo.Clone(stlPageContentsElement, stlElementInfo.InnerHtml, stlElementInfo.Attributes);

            _listInfo = ListInfo.GetListInfo(_pageInfo, _contextInfo, EContextType.Content);

            _listInfo.Scope = EScopeType.All;

            if (pageNum > 0)
            {
                _listInfo.PageNum = pageNum;
            }

            _sqlString = StlDataUtility.GetPageContentsSqlStringBySearch(channelInfo, _listInfo.GroupContent, _listInfo.GroupContentNot, _listInfo.Tags, _listInfo.IsImageExists, _listInfo.IsImage, _listInfo.IsVideoExists, _listInfo.IsVideo, _listInfo.IsFileExists, _listInfo.IsFile, _listInfo.StartNum, _listInfo.TotalNum, _listInfo.OrderByString, _listInfo.IsTopExists, _listInfo.IsTop, _listInfo.IsRecommendExists, _listInfo.IsRecommend, _listInfo.IsHotExists, _listInfo.IsHot, _listInfo.IsColorExists, _listInfo.IsColor, whereString);
        }

        public int GetPageCount(out int totalNum)
        {
            totalNum = 0;
            var pageCount = 1;
            try
            {
                //totalNum = DatabaseUtils.GetPageTotalCount(SqlString);
                totalNum = StlDatabaseCache.GetPageTotalCount(_sqlString);
                if (_listInfo.PageNum != 0 && _listInfo.PageNum < totalNum)//需要翻页
                {
                    pageCount = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(totalNum) / Convert.ToDouble(_listInfo.PageNum)));//需要生成的总页数
                }
            }
            catch (Exception ex)
            {
                LogUtils.AddStlErrorLog(_pageInfo, ElementName, _stlPageContentsElement, ex);
            }
            return pageCount;
        }

        public string Parse(int totalNum, int currentPageIndex, int pageCount, bool isStatic)
        {
            if (isStatic)
            {
                var maxPage = _listInfo.MaxPage;
                if (maxPage == 0)
                {
                    maxPage = _pageInfo.SiteInfo.CreateStaticMaxPage;
                }
                if (maxPage > 0 && currentPageIndex + 1 > maxPage)
                {
                    return ParseDynamic(totalNum, currentPageIndex, pageCount);
                }
            }

            var parsedContent = string.Empty;

            _contextInfo.PageItemIndex = currentPageIndex * _listInfo.PageNum;

            try
            {
                if (!string.IsNullOrEmpty(_sqlString))
                {
                    var contentList = StlContentCache.GetContainerContentListBySqlString(_contextInfo.ChannelInfo, _sqlString, _listInfo.OrderByString, totalNum, _listInfo.PageNum, currentPageIndex);
                    parsedContent = StlContents.ParseElement(_pageInfo, _contextInfo, _listInfo, contentList);

                    // var pageSqlString = StlDatabaseCache.GetStlPageSqlString(_sqlString, _listInfo.OrderByString, totalNum, _listInfo.PageNum, currentPageIndex);
                    // var datasource = DatabaseUtils.GetDataSource(pageSqlString);

                    // if (_listInfo.Layout == ELayout.None)
                    // {
                    //     var rptContents = new Repeater();

                    //     if (!string.IsNullOrEmpty(_listInfo.HeaderTemplate))
                    //     {
                    //         rptContents.HeaderTemplate = new SeparatorTemplate(_listInfo.HeaderTemplate);
                    //     }
                    //     if (!string.IsNullOrEmpty(_listInfo.FooterTemplate))
                    //     {
                    //         rptContents.FooterTemplate = new SeparatorTemplate(_listInfo.FooterTemplate);
                    //     }
                    //     if (!string.IsNullOrEmpty(_listInfo.SeparatorTemplate))
                    //     {
                    //         rptContents.SeparatorTemplate = new SeparatorTemplate(_listInfo.SeparatorTemplate);
                    //     }
                    //     if (!string.IsNullOrEmpty(_listInfo.AlternatingItemTemplate))
                    //     {
                    //         rptContents.AlternatingItemTemplate = new RepeaterTemplate(_listInfo.AlternatingItemTemplate, _listInfo.SelectedItems, _listInfo.SelectedValues, _listInfo.SeparatorRepeatTemplate, _listInfo.SeparatorRepeat, _pageInfo, EContextType.Content, _contextInfo);
                    //     }

                    //     rptContents.ItemTemplate = new RepeaterTemplate(_listInfo.ItemTemplate, _listInfo.SelectedItems, _listInfo.SelectedValues, _listInfo.SeparatorRepeatTemplate, _listInfo.SeparatorRepeat, _pageInfo, EContextType.Content, _contextInfo);

                    //     rptContents.DataSource = datasource;
                    //     rptContents.DataBind();

                    //     if (rptContents.Items.Count > 0)
                    //     {
                    //         parsedContent = ControlUtils.GetControlRenderHtml(rptContents);
                    //     }
                    // }
                    // else
                    // {
                    //     var pdlContents = new ParsedDataList();

                    //     //设置显示属性
                    //     TemplateUtility.PutListInfoToMyDataList(pdlContents, _listInfo);

                    //     pdlContents.ItemTemplate = new DataListTemplate(_listInfo.ItemTemplate, _listInfo.SelectedItems, _listInfo.SelectedValues, _listInfo.SeparatorRepeatTemplate, _listInfo.SeparatorRepeat, _pageInfo, EContextType.Content, _contextInfo);
                    //     if (!string.IsNullOrEmpty(_listInfo.HeaderTemplate))
                    //     {
                    //         pdlContents.HeaderTemplate = new SeparatorTemplate(_listInfo.HeaderTemplate);
                    //     }
                    //     if (!string.IsNullOrEmpty(_listInfo.FooterTemplate))
                    //     {
                    //         pdlContents.FooterTemplate = new SeparatorTemplate(_listInfo.FooterTemplate);
                    //     }
                    //     if (!string.IsNullOrEmpty(_listInfo.SeparatorTemplate))
                    //     {
                    //         pdlContents.SeparatorTemplate = new SeparatorTemplate(_listInfo.SeparatorTemplate);
                    //     }
                    //     if (!string.IsNullOrEmpty(_listInfo.AlternatingItemTemplate))
                    //     {
                    //         pdlContents.AlternatingItemTemplate = new DataListTemplate(_listInfo.AlternatingItemTemplate, _listInfo.SelectedItems, _listInfo.SelectedValues, _listInfo.SeparatorRepeatTemplate, _listInfo.SeparatorRepeat, _pageInfo, EContextType.Content, _contextInfo);
                    //     }

                    //     pdlContents.DataSource = datasource;
                    //     pdlContents.DataKeyField = ContentAttribute.Id;
                    //     pdlContents.DataBind();

                    //     if (pdlContents.Items.Count > 0)
                    //     {
                    //         parsedContent = ControlUtils.GetControlRenderHtml(pdlContents);
                    //     }
                    // }
                }
            }
            catch (Exception ex)
            {
                parsedContent = LogUtils.AddStlErrorLog(_pageInfo, ElementName, _stlPageContentsElement, ex);
            }

            //还原翻页为0，使得其他列表能够正确解析ItemIndex
            _contextInfo.PageItemIndex = 0;
            return parsedContent;
        }

        private string ParseDynamic(int totalNum, int currentPageIndex, int pageCount)
        {
            var loading = _listInfo.LoadingTemplate;
            if (string.IsNullOrEmpty(loading))
            {
                loading = @"<div style=""margin: 0 auto;
    padding: 40px 0;
    font-size: 14px;
    font-family: 'Microsoft YaHei';
    text-align: center;
    font-weight: 400;"">
        载入中，请稍后...
</div>";
            }

            _pageInfo.AddPageBodyCodeIfNotExists(PageInfo.Const.Jquery);

            var ajaxDivId = StlParserUtility.GetAjaxDivId(_pageInfo.UniqueId);
            var apiUrl = ApiRouteActionsPageContents.GetUrl(_pageInfo.ApiUrl);
            var apiParameters = ApiRouteActionsPageContents.GetParameters(_pageInfo.SiteId, _pageInfo.PageChannelId, _pageInfo.TemplateInfo.Id, totalNum, pageCount, currentPageIndex, _stlPageContentsElement);

            var builder = new StringBuilder();
            builder.Append($@"<div id=""{ajaxDivId}"">");
            builder.Append($@"<div class=""loading"">{loading}</div>");
            builder.Append($@"<div class=""yes"">{string.Empty}</div>");
            builder.Append("</div>");

            builder.Append($@"
<script type=""text/javascript"" language=""javascript"">
$(document).ready(function(){{
    $(""#{ajaxDivId} .loading"").show();
    $(""#{ajaxDivId} .yes"").hide();

    var url = '{apiUrl}';
    var parameters = {apiParameters};

    $.support.cors = true;
    $.ajax({{
        url: url,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(parameters),
        dataType: 'json',
        success: function(res) {{
            $(""#{ajaxDivId} .loading"").hide();
            $(""#{ajaxDivId} .yes"").show();
            $(""#{ajaxDivId} .yes"").html(res);
        }},
        error: function(e) {{
            $(""#{ajaxDivId} .loading"").hide();
            $(""#{ajaxDivId} .yes"").hide();
        }}
    }});
}});
</script>
");

            return builder.ToString();
        }
    }
}