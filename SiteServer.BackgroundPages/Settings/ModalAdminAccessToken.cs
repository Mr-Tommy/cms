﻿using System;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.BackgroundPages.Core;
using SiteServer.CMS.Core;
using SiteServer.Utils;

namespace SiteServer.BackgroundPages.Settings
{
    public class ModalAdminAccessToken : BasePage
    {
        public static readonly string PageUrl = PageUtilsEx.GetSettingsUrl(nameof(ModalAdminAccessToken));

        public Literal LtlTitle;
        public Literal LtlToken;
        public Literal LtlAddDate;
        public Literal LtlUpdatedDate;

        private int _id;

        public static string GetOpenWindowString(int id)
        {
            return LayerUtils.GetOpenScript("获取密钥", PageUtilsEx.GetSettingsUrl(nameof(ModalAdminAccessToken), new NameValueCollection
            {
                {"id", id.ToString()}
            }), 0, 420);
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            _id = AuthRequest.GetQueryInt("id");

            if (IsPostBack) return;

            var tokenInfo = DataProvider.AccessTokenDao.Get(_id);

            LtlTitle.Text = tokenInfo.Title;
            LtlToken.Text = TranslateUtils.DecryptStringBySecretKey(tokenInfo.Token);
            LtlAddDate.Text = DateUtils.GetDateAndTimeString(tokenInfo.AddDate);
            LtlUpdatedDate.Text = DateUtils.GetDateAndTimeString(tokenInfo.LastModifiedDate);
        }

        public void Regenerate_OnClick(object sender, EventArgs e)
        {
            if (!IsPostBack || !IsValid) return;

            try
            {
                var accessTokenInfo = DataProvider.AccessTokenDao.Get(_id);
                LtlToken.Text = TranslateUtils.DecryptStringBySecretKey(DataProvider.AccessTokenDao.Regenerate(accessTokenInfo));
                LtlUpdatedDate.Text = DateUtils.GetDateAndTimeString(DateTime.Now);

                AuthRequest.AddAdminLog("重设API密钥");

                SuccessMessage("API密钥重新设置成功！");
            }
            catch (Exception ex)
            {
                FailMessage(ex, "API密钥重新设置失败！");
            }
        }
    }
}
