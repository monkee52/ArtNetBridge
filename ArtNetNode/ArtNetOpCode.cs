﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AydenIO.ArtNet.Node {
    public enum ArtNetOpCode : ushort {
        OpPoll = 0x2000,
        OpPollReply = 0x2100,
        OpOutput = 0x5000
    }
}
