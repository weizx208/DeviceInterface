﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECore.DeviceMemories
{
    public enum MAX19506
    {
        POWER_MANAGEMENT = 0,
        OUTPUT_FORMAT = 1,
        OUTPUT_PWR_MNGMNT = 2,
        DATA_CLK_TIMING = 3,
        CHA_TERMINATION = 4,
        CHB_TERMINATION = 5,
        FORMAT_PATTERN = 6,
        COMMON_MODE = 8,
        SOFT_RESET = 10,
    }
    //this class defines which type of registers it contain, how much of them, and how to access them
    //actual filling of these registers must be defined by the specific HWImplementation, through the constructor of this class
    public class MAX19506Memory : ByteMemory
    {
        private ScopeFpgaSettingsMemory fpgaSettings;
        private ScopeStrobeMemory strobeMemory;
        private ScopeFpgaRom fpgaRom;

        public MAX19506Memory(ScopeFpgaSettingsMemory fpgaMemory, ScopeStrobeMemory strobeMemory, ScopeFpgaRom fpgaRom)
        {
            this.fpgaSettings = fpgaMemory;
            this.strobeMemory = strobeMemory;
            this.fpgaRom = fpgaRom;

            foreach (MAX19506 reg in Enum.GetValues(typeof(MAX19506)))
                registers.Add((uint)reg, new ByteRegister(this, (uint)reg, reg.ToString()));
        }

        public override void Read(uint address, uint length)
        {
            for (uint i = 0; i < length; i++)
            {
                fpgaSettings.GetRegister(REG.SPI_ADDRESS).Set(address + i + 128); //for a read, MSB must be 1
                fpgaSettings.WriteSingle(REG.SPI_ADDRESS);

                //next, trigger rising edge to initiate SPI comm
                strobeMemory.GetRegister(STR.INIT_SPI_TRANSFER).Set(false).Write();
                strobeMemory.GetRegister(STR.INIT_SPI_TRANSFER).Set(true).Write();

                //finally read acquired value
                int acquiredVal = fpgaRom.GetRegister(ROM.SPI_RECEIVED_VALUE).Read().GetByte();
                Registers[address + i].Set(acquiredVal);
            }            
            
        }

        public override void Write(uint address, uint length)
        {
            for (uint i = 0; i < length; i++)
            {
                //first send correct address to FPGA
                fpgaSettings.GetRegister(REG.SPI_ADDRESS).Set(address + i);
                fpgaSettings.WriteSingle(REG.SPI_ADDRESS);

                //next, send the write value to FPGA
                int valToWrite = GetRegister(address + i).GetByte();
                fpgaSettings.GetRegister(REG.SPI_WRITE_VALUE).Set(valToWrite);
                fpgaSettings.WriteSingle(REG.SPI_WRITE_VALUE);

                //finally, trigger rising edge
                strobeMemory.GetRegister(STR.INIT_SPI_TRANSFER).Set(false).Write();
                strobeMemory.GetRegister(STR.INIT_SPI_TRANSFER).Set(true).Write();
            }
        }
        public void WriteSingle(MAX19506 r)
        {
            this.WriteSingle((uint)r);
        }
        public void ReadSingle(MAX19506 r)
        {
            this.ReadSingle((uint)r);
        }
        public ByteRegister GetRegister(MAX19506 r)
        {
            return GetRegister((uint)r);
        }
    }
}
