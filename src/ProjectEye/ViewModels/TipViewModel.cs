﻿using ProjectEye.Core;
using ProjectEye.Core.Service;
using ProjectEye.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace ProjectEye.ViewModels
{
    public class TipViewModel : TipModel
    {
        /// <summary>
        /// 休息命令
        /// </summary>
        public Command resetCommand { get; set; }
        /// <summary>
        /// 跳过命令
        /// </summary>
        public Command busyCommand { get; set; }

        private readonly ResetService reset;
        private readonly SoundService sound;
        private readonly ConfigService config;
        private readonly StatisticService statistic;
        private readonly MainService main;

        private string tipContent;
        public TipViewModel(ResetService reset,
            SoundService sound,
            ConfigService config,
            StatisticService statistic,
            MainService main,
            App app)
        {
            this.reset = reset;
            this.reset.TimeChanged += new ResetEventHandler(timeChanged);
            this.reset.ResetCompleted += new ResetEventHandler(resetCompleted);

            this.sound = sound;
            this.config = config;
            this.config.Changed += config_Changed;


            resetCommand = new Command(new Action<object>(resetCommand_action));
            busyCommand = new Command(new Action<object>(busyCommand_action));

            this.statistic = statistic;

            this.main = main;

            app.OnServiceInitialized += App_OnServiceInitialized;

            LoadConfig();

        }

        private void App_OnServiceInitialized()
        {
            WindowsListener();
        }

        //加载配置
        private void LoadConfig()
        {
            tipContent = config.options.Style.TipContent;
        }

        //配置文件被修改时
        private void config_Changed(object sender, EventArgs e)
        {
            LoadConfig();
        }

        private void resetCompleted(object sender, int timed)
        {
            //休息结束
            Init();
            //播放提示音
            if (config.options.General.Sound)
            {
                sound.Play();
            }
        }

        private void Init()
        {
            CountDown = 20;
            CountDownVisibility = System.Windows.Visibility.Hidden;
            TakeButtonVisibility = System.Windows.Visibility.Visible;
        }

        private void resetCommand_action(object obj)
        {
            main.StopBusyListener();
            CountDownVisibility = System.Windows.Visibility.Visible;
            TakeButtonVisibility = System.Windows.Visibility.Hidden;
            reset.Start();
            if (config.options.General.Data)
            {
                statistic.Add(StatisticType.ResetTime, 20);
            }
        }
        private void busyCommand_action(object obj)
        {
            main.StopBusyListener();
            WindowManager.Hide("TipWindow");
            if (config.options.General.Data)
            {
                statistic.Add(StatisticType.SkipCount, 1);
            }
        }
        private void timeChanged(object sender, int timed)
        {
            CountDown = timed;

        }

        /// <summary>
        /// 解析提示文本中的变量
        /// </summary>
        /// <param name="tipContent"></param>
        /// <returns></returns>
        private string ParseTipContent(string tipContent)
        {
            string pattern = @"\{(?<value>[a-zA-Z]*?)\}";
            var variableArray = Regex.Matches(tipContent, pattern)
                 .OfType<Match>()
                 .Select(m => m.Value)
                 .Distinct();
            foreach (string variable in variableArray)
            {
                Debug.WriteLine(variable);
                string replace = "";
                switch (variable)
                {
                    case "{t}":
                        //提醒间隔变量
                        replace = config.options.General.WarnTime.ToString();
                        break;
                    case "{time}":
                        //当前时间
                        replace = DateTime.Now.ToString();
                        break;
                    case "{y}":
                        //年
                        replace = DateTime.Now.ToString("yyyy");
                        break;
                    case "{M}":
                        //年
                        replace = DateTime.Now.ToString("MM");
                        break;
                    case "{d}":
                        //年
                        replace = DateTime.Now.ToString("dd");
                        break;
                    case "{H}":
                        //年
                        replace = DateTime.Now.ToString("HH");
                        break;
                    case "{m}":
                        //年
                        replace = DateTime.Now.ToString("mm");
                        break;
                }
                if (!string.IsNullOrEmpty(replace))
                {
                    tipContent = tipContent.Replace(variable, replace);
                }
            }
            return tipContent;
        }
        /// <summary>
        /// 窗口监听
        /// </summary>
        private void WindowsListener()
        {
            var windows = WindowManager.GetWindows("TipWindow");
            foreach (var window in windows)
            {
                window.Activated += Window_Activated;
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            TipContent = ParseTipContent(tipContent);
        }
    }
}
