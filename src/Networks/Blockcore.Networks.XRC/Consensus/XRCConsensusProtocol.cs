﻿using Blockcore.Consensus;

namespace Blockcore.Networks.XRC.Consensus
{
    public class XRCConsensusProtocol : ConsensusProtocol
    {
        public int PowLimit2Height { get; set; }
        public uint PowLimit2Time { get; set; }
        public int PowDigiShieldX11Height { get; set; }
        public uint PowX11Time { get; set; }
        public int PowDarkGravityWaveHeight { get; set; }
        public int PowDarkGravityWaveTime { get; set; }

    }
}
