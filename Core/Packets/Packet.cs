using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace QuantumCore.Core.Packets
{
    [Flags]
    public enum EDirection
    {
        Incoming = 1,
        Outgoing = 2
    }

    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class Packet : Attribute
    {
        public Packet(byte header, EDirection direction)
        {
            Header = header;
            Direction = direction;
        }

        public byte Header { get; set; }
        public EDirection Direction { get; set; }
        public bool Sequence { get; set; }
    }

    public class PacketCache
    {
        private readonly List<PropertyInfo> _properties = new List<PropertyInfo>();

        public PacketCache(byte header, Type type)
        {
            Header = header;
            Type = type;

            CalculateSize();
        }

        public byte Header { get; }
        public Type Type { get; }
        public uint Size { get; private set; }

        public byte[] Serialize(object obj)
        {
            if (obj.GetType() != Type) throw new ArgumentException("Invalid packet given", nameof(obj));

            var ret = new byte[Size];
            using (var ms = new MemoryStream(ret))
            {
                using (var bw = new BinaryWriter(ms))
                {
                    // Write header
                    bw.Write(Header);

                    foreach (var field in _properties)
                    {
                        var type = field.PropertyType;
                        if (type == typeof(uint))
                            bw.Write((uint) field.GetValue(obj));
                        else if (type == typeof(byte))
                            bw.Write((byte) field.GetValue(obj));
                        else
                            Debug.Assert(false);
                    }
                }
            }

            return ret;
        }

        public void Deserialize(object obj, byte[] data)
        {
            if (data.Length != Size - 1) throw new ArgumentException("Invalid data stream given", nameof(data));
            if (obj.GetType() != Type) throw new ArgumentException("Invalid packet given", nameof(obj));

            using (var ms = new MemoryStream(data))
            {
                using (var br = new BinaryReader(ms))
                {
                    foreach (var field in _properties)
                    {
                        var type = field.PropertyType;
                        var attribute = field.GetCustomAttribute<Field>();
                        var multiplier = 1;
                        Array array = null;

                        if (type.IsArray)
                        {
                            type = type.GetElementType();
                            multiplier = attribute.ArrayLength;
                            array = Array.CreateInstance(type, multiplier);
                        }

                        for (var i = 0; i < multiplier; i++)
                        {
                            object value = null;
                            if (type == typeof(uint))
                            {
                                value = br.ReadUInt32();
                            }
                            else if (type == typeof(byte))
                            {
                                value = br.ReadByte();
                            }
                            else if (type == typeof(string))
                            {
                                var chars = br.ReadChars(attribute.Length);
                                var idx = Array.IndexOf(chars, '\0');
                                value = new string(chars, 0, idx < 0 ? chars.Length : idx);
                            }
                            else
                            {
                                Debug.Assert(false);
                            }

                            if (array != null)
                                array.SetValue(value, i);
                            else
                                field.SetValue(obj, value);
                        }

                        if (array != null) field.SetValue(obj, array);
                    }
                }
            }
        }

        private void CalculateSize()
        {
            var fields = Type.GetProperties().Where(field => field.GetCustomAttribute<Field>() != null)
                .OrderBy(field => field.GetCustomAttribute<Field>().Position);
            Size = 1;
            var packetAttribute = Type.GetCustomAttribute<Packet>();
            if (packetAttribute == null) return;
            if (packetAttribute.Sequence) Size++;
            
            foreach (var field in fields)
            {
                var type = field.PropertyType;
                var attribute = field.GetCustomAttribute<Field>();
                uint multiplier = 1;

                if (type.IsArray)
                {
                    type = type.GetElementType();
                    multiplier = (uint) attribute.ArrayLength;
                }

                if (type == typeof(uint))
                {
                    Size += 4 * multiplier;
                }
                else if (type == typeof(byte))
                {
                    Size += 1 * multiplier;
                }
                else if (type == typeof(string))
                {
                    Debug.Assert(attribute.Length > 0);
                    Size += (uint) attribute.Length * multiplier;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Packet {Type.Name} contains invalid type {type.Name} for property {field.Name}!");
                }

                _properties.Add(field);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Field : Attribute
    {
        public Field(int position)
        {
            Position = position;
        }

        public int Position { get; set; }
        public int Length { get; set; } = -1;
        public int ArrayLength { get; set; } = -1;
    }
}