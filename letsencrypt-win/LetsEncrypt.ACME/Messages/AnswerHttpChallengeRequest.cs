﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Messages
{
    public class AnswerHttpChallengeRequest : RequestMessage
    {
        public AnswerHttpChallengeRequest() : base("challenge")
        { }

        public string Type
        { get; private set; } = "simpleHttp";

        public bool Tls
        { get; set; }
    }
}
