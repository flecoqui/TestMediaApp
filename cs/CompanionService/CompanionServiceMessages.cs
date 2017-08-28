//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace CompanionService
{
    public static class CompanionServiceMessage
    {



        public static string ATT_TYPE { get { return "type"; } }
        public static string TYPE_INIT { get { return  "init"; }}
        public static string TYPE_RESULT { get { return "result"; }}
        public static string TYPE_DATA { get { return "data"; }}
        public static string ATT_MESSAGE { get { return "message"; }}
        public static string ATT_SOURCEID { get { return "sourceid"; }}
        public static string ATT_SOURCEIP { get { return "sourceip"; }}
        public static string ATT_SOURCENAME { get { return "sourcename"; } }
        public static string ATT_SOURCEKIND { get { return "sourcekind"; } }
        public static string ATT_RESULT { get { return "result"; }}
        public static string VAL_RESULT_OK { get { return "OK"; }}
        public static string VAL_RESULT_ERROR { get { return "ERROR"; }}
    }

}
