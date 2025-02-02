﻿using Microsoft.Win32;
using SteamTools.Models;
using SteamTools.Models.Settings;
using SteamTools.Properties;
using SteamTools.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using SteamTool.Core.Common;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WinAuth;
using System.Net;
using SteamTools.Win32;
using SteamTool.Model;
using System.Drawing;

namespace SteamTools.ViewModels
{
    public class AuthTradeWindowViewModel : MainWindowViewModelBase
    {
        private readonly SteamAuthenticator _AuthenticatorData;
        private readonly WinAuthAuthenticator _Authenticator;

        public AuthTradeWindowViewModel(WinAuthAuthenticator auth)
        {
            _Authenticator = auth;
            _AuthenticatorData = auth.AuthenticatorData as SteamAuthenticator;
            this.Title = ProductInfo.Title + " | " + Resources.Auth_TradeTitle;


            //var steam = _Authenticator.GetClient();
            //if (steam.IsLoggedIn() == false)
            //{
            //    ExtractSteamCookies(steam);
            //}
        }

        #region LoginData
        private string _UserName;
        public string UserName
        {
            get => this._UserName;
            set
            {
                if (this._UserName != value)
                {
                    this._UserName = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private string _Password;
        public string Password
        {
            get => this._Password;
            set
            {
                if (this._Password != value)
                {
                    this._Password = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private bool _RememberMe;
        public bool RememberMe
        {
            get => this._RememberMe;
            set
            {
                if (this._RememberMe != value)
                {
                    this._RememberMe = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private string _CodeImage;
        public string CodeImage
        {
            get => this._CodeImage;
            set
            {
                if (this._CodeImage != value)
                {
                    this._CodeImage = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private string _CodeImageChar;
        public string CodeImageChar
        {
            get => this._CodeImageChar;
            set
            {
                if (this._CodeImageChar != value)
                {
                    this._CodeImageChar = value;
                    this.RaisePropertyChanged();
                }
            }
        }
        #endregion

        public bool IsLoggedIn => this._AuthenticatorData.GetClient().IsLoggedIn();
        public bool IsRequiresCaptcha => this._AuthenticatorData.GetClient().RequiresCaptcha;

        /// <summary>
        /// Trade info state
        /// </summary>
        private List<SteamClient.Confirmation> _Confirmations;
        public List<SteamClient.Confirmation> Confirmations
        {
            get => this._Confirmations;
            set
            {
                if (this._Confirmations != value)
                {
                    this._Confirmations = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public void LoginButton_Click()
        {
            if (UserName?.Trim().Length > 0 || Password?.Trim().Length > 0)
            {
                Process();
            }
            else
            {
                this.Dialog("请输入您的账号和密码");
                return;
            }

        }

        private void Init()
        {
            if (IsLoggedIn)
            {
                Process();
            }
        }

        private void Process(string captchaId = null)
        {
            var steam = _AuthenticatorData.GetClient();

            if (!steam.IsLoggedIn())
            {
                if (steam.Login(UserName, Password, captchaId, CodeImageChar, ResourceService.Current.GetCurrentCultureSteamLanguageName()) == false)
                {
                    if (steam.Error == "Incorrect Login")
                    {
                        this.Dialog("账号或密码错误");
                        return;
                    }
                    if (steam.Requires2FA == true)
                    {
                        this.Dialog("无效验证码：您确定这是您账户的当前验证码？");
                        return;
                    }

                    if (steam.RequiresCaptcha == true)
                    {
                        this.Dialog("请输入图片验证码");
                        CodeImage = steam.CaptchaUrl;
                        return;
                    }
                    //loginButton.Enabled = true;
                    //captchaGroup.Visible = false;

                    if (string.IsNullOrEmpty(steam.Error) == false)
                    {
                        this.Dialog(steam.Error);
                        return;
                    }

                    return;
                }

                _AuthenticatorData.SessionData = (RememberMe ? steam.Session.ToString() : null);
                AuthService.Current.SaveCurrentAuth();
            }

            try
            {
                Confirmations = steam.GetConfirmations();

                // 获取新交易后保存
                if (!string.IsNullOrEmpty(_AuthenticatorData.SessionData))
                {
                    AuthService.Current.SaveCurrentAuth();
                }
            }
            catch (SteamClient.UnauthorisedSteamRequestException)
            {
                // Family view is probably on
                this.Dialog("You are not allowed to view confirmations. Have you enabled 'community-generated content' in Family View?");
                return;
            }
            catch (SteamClient.InvalidSteamRequestException)
            {
                // likely a bad session so try a refresh first
                try
                {
                    steam.Refresh();
                    Confirmations = steam.GetConfirmations();
                }
                catch (Exception)
                {
                    // reset and show normal login
                    steam.Clear();
                    return;
                }
            }
        }
    }
}
