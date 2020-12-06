using QuantumCore.Core.Packets;

namespace QuantumCore.Game.Packets
{
    [Packet(0x15, EDirection.Outgoing)]
    public class SetItem
    {
        public class Bonus
        {
            [Field(0)]
            public byte BonusId { get; set; }
            [Field(1)]
            public ushort Value { get; set; }
        }
        
        [Field(0)]
        public byte Window { get; set; }
        [Field(1)]
        public ushort Position { get; set; }
        [Field(2)]
        public uint ItemId { get; set; }
        [Field(3)]
        public byte Count { get; set; }
        [Field(4)]
        public uint Flags { get; set; }
        [Field(5)]
        public uint AnitFlags { get; set; }
        [Field(6)]
        public uint Highlight { get; set; }
        [Field(7, ArrayLength = 3)]
        public uint[] Sockets { get; set; } = new uint[3];
        [Field(8, ArrayLength = 7)]
        public Bonus[] Bonuses { get; set; } = new Bonus[7];
    }
}