﻿// FFXIVAPP.Common
// Logging.cs
//  
// Created by Ryan Wilson.
// Copyright © 2007-2013 Ryan Wilson - All Rights Reserved

#region Usings

using System;
using NLog;

#endregion

namespace FFXIVAPP.Common.Utilities
{
    public static class Logging
    {
        public static void Log(Logger logger, string message = "", Exception ex = null)
        {
            if (!Constants.EnableNLog)
            {
                return;
            }
            message = message == "" ? " :: Log Message Undefined :: " : message;
            if (ex == null)
            {
                logger.Trace("HandlingEvent : {0}\n\n", message);
                return;
            }
            logger.Error("HandlingEvent : {0} ::\n Extended Info ::\n{1}\n{2}\n\n", message, ex.Message, ex.StackTrace);
        }
    }
}