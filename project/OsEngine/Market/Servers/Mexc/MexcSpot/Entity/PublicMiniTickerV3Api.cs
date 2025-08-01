﻿// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: PublicMiniTickerV3Api.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from PublicMiniTickerV3Api.proto</summary>
public static partial class PublicMiniTickerV3ApiReflection
{

    #region Descriptor
    /// <summary>File descriptor for PublicMiniTickerV3Api.proto</summary>
    public static pbr::FileDescriptor Descriptor
    {
        get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static PublicMiniTickerV3ApiReflection()
    {
        byte[] descriptorData = global::System.Convert.FromBase64String(
            string.Concat(
              "ChtQdWJsaWNNaW5pVGlja2VyVjNBcGkucHJvdG8i9AEKFVB1YmxpY01pbmlU",
              "aWNrZXJWM0FwaRIOCgZzeW1ib2wYASABKAkSDQoFcHJpY2UYAiABKAkSDAoE",
              "cmF0ZRgDIAEoCRIRCgl6b25lZFJhdGUYBCABKAkSDAoEaGlnaBgFIAEoCRIL",
              "CgNsb3cYBiABKAkSDgoGdm9sdW1lGAcgASgJEhAKCHF1YW50aXR5GAggASgJ",
              "EhUKDWxhc3RDbG9zZVJhdGUYCSABKAkSGgoSbGFzdENsb3NlWm9uZWRSYXRl",
              "GAogASgJEhUKDWxhc3RDbG9zZUhpZ2gYCyABKAkSFAoMbGFzdENsb3NlTG93",
              "GAwgASgJQj4KHGNvbS5teGMucHVzaC5jb21tb24ucHJvdG9idWZCGlB1Ymxp",
              "Y01pbmlUaWNrZXJWM0FwaVByb3RvSAFQAWIGcHJvdG8z"));
        descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
            new pbr::FileDescriptor[] { },
            new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::PublicMiniTickerV3Api), global::PublicMiniTickerV3Api.Parser, new[]{ "Symbol", "Price", "Rate", "ZonedRate", "High", "Low", "Volume", "Quantity", "LastCloseRate", "LastCloseZonedRate", "LastCloseHigh", "LastCloseLow" }, null, null, null, null)
            }));
    }
    #endregion

}
#region Messages
[global::System.Diagnostics.DebuggerDisplayAttribute("{ToString(),nq}")]
public sealed partial class PublicMiniTickerV3Api : pb::IMessage<PublicMiniTickerV3Api>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
    private static readonly pb::MessageParser<PublicMiniTickerV3Api> _parser = new pb::MessageParser<PublicMiniTickerV3Api>(() => new PublicMiniTickerV3Api());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<PublicMiniTickerV3Api> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor
    {
        get { return global::PublicMiniTickerV3ApiReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor
    {
        get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public PublicMiniTickerV3Api()
    {
        OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public PublicMiniTickerV3Api(PublicMiniTickerV3Api other) : this()
    {
        symbol_ = other.symbol_;
        price_ = other.price_;
        rate_ = other.rate_;
        zonedRate_ = other.zonedRate_;
        high_ = other.high_;
        low_ = other.low_;
        volume_ = other.volume_;
        quantity_ = other.quantity_;
        lastCloseRate_ = other.lastCloseRate_;
        lastCloseZonedRate_ = other.lastCloseZonedRate_;
        lastCloseHigh_ = other.lastCloseHigh_;
        lastCloseLow_ = other.lastCloseLow_;
        _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public PublicMiniTickerV3Api Clone()
    {
        return new PublicMiniTickerV3Api(this);
    }

    /// <summary>Field number for the "symbol" field.</summary>
    public const int SymbolFieldNumber = 1;
    private string symbol_ = "";
    /// <summary>
    /// 交易对名
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Symbol
    {
        get { return symbol_; }
        set
        {
            symbol_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "price" field.</summary>
    public const int PriceFieldNumber = 2;
    private string price_ = "";
    /// <summary>
    /// 最新价格
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Price
    {
        get { return price_; }
        set
        {
            price_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "rate" field.</summary>
    public const int RateFieldNumber = 3;
    private string rate_ = "";
    /// <summary>
    /// utc+8时区涨跌幅
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Rate
    {
        get { return rate_; }
        set
        {
            rate_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "zonedRate" field.</summary>
    public const int ZonedRateFieldNumber = 4;
    private string zonedRate_ = "";
    /// <summary>
    /// 时区涨跌幅
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string ZonedRate
    {
        get { return zonedRate_; }
        set
        {
            zonedRate_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "high" field.</summary>
    public const int HighFieldNumber = 5;
    private string high_ = "";
    /// <summary>
    /// 滚动最高价
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string High
    {
        get { return high_; }
        set
        {
            high_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "low" field.</summary>
    public const int LowFieldNumber = 6;
    private string low_ = "";
    /// <summary>
    /// 滚动最低价
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Low
    {
        get { return low_; }
        set
        {
            low_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "volume" field.</summary>
    public const int VolumeFieldNumber = 7;
    private string volume_ = "";
    /// <summary>
    /// 滚动成交额
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Volume
    {
        get { return volume_; }
        set
        {
            volume_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "quantity" field.</summary>
    public const int QuantityFieldNumber = 8;
    private string quantity_ = "";
    /// <summary>
    /// 滚动成交量
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Quantity
    {
        get { return quantity_; }
        set
        {
            quantity_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "lastCloseRate" field.</summary>
    public const int LastCloseRateFieldNumber = 9;
    private string lastCloseRate_ = "";
    /// <summary>
    /// utc+8时区上期收盘价模式涨跌幅
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string LastCloseRate
    {
        get { return lastCloseRate_; }
        set
        {
            lastCloseRate_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "lastCloseZonedRate" field.</summary>
    public const int LastCloseZonedRateFieldNumber = 10;
    private string lastCloseZonedRate_ = "";
    /// <summary>
    /// 上期收盘价模式时区涨跌幅
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string LastCloseZonedRate
    {
        get { return lastCloseZonedRate_; }
        set
        {
            lastCloseZonedRate_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "lastCloseHigh" field.</summary>
    public const int LastCloseHighFieldNumber = 11;
    private string lastCloseHigh_ = "";
    /// <summary>
    /// 上期收盘价模式滚动最高价
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string LastCloseHigh
    {
        get { return lastCloseHigh_; }
        set
        {
            lastCloseHigh_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    /// <summary>Field number for the "lastCloseLow" field.</summary>
    public const int LastCloseLowFieldNumber = 12;
    private string lastCloseLow_ = "";
    /// <summary>
    /// 上期收盘价模式滚动最低价
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string LastCloseLow
    {
        get { return lastCloseLow_; }
        set
        {
            lastCloseLow_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
        }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other)
    {
        return Equals(other as PublicMiniTickerV3Api);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(PublicMiniTickerV3Api other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }
        if (ReferenceEquals(other, this))
        {
            return true;
        }
        if (Symbol != other.Symbol) return false;
        if (Price != other.Price) return false;
        if (Rate != other.Rate) return false;
        if (ZonedRate != other.ZonedRate) return false;
        if (High != other.High) return false;
        if (Low != other.Low) return false;
        if (Volume != other.Volume) return false;
        if (Quantity != other.Quantity) return false;
        if (LastCloseRate != other.LastCloseRate) return false;
        if (LastCloseZonedRate != other.LastCloseZonedRate) return false;
        if (LastCloseHigh != other.LastCloseHigh) return false;
        if (LastCloseLow != other.LastCloseLow) return false;
        return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode()
    {
        int hash = 1;
        if (Symbol.Length != 0) hash ^= Symbol.GetHashCode();
        if (Price.Length != 0) hash ^= Price.GetHashCode();
        if (Rate.Length != 0) hash ^= Rate.GetHashCode();
        if (ZonedRate.Length != 0) hash ^= ZonedRate.GetHashCode();
        if (High.Length != 0) hash ^= High.GetHashCode();
        if (Low.Length != 0) hash ^= Low.GetHashCode();
        if (Volume.Length != 0) hash ^= Volume.GetHashCode();
        if (Quantity.Length != 0) hash ^= Quantity.GetHashCode();
        if (LastCloseRate.Length != 0) hash ^= LastCloseRate.GetHashCode();
        if (LastCloseZonedRate.Length != 0) hash ^= LastCloseZonedRate.GetHashCode();
        if (LastCloseHigh.Length != 0) hash ^= LastCloseHigh.GetHashCode();
        if (LastCloseLow.Length != 0) hash ^= LastCloseLow.GetHashCode();
        if (_unknownFields != null)
        {
            hash ^= _unknownFields.GetHashCode();
        }
        return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString()
    {
        return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output)
    {
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
        output.WriteRawMessage(this);
#else
    if (Symbol.Length != 0) {
      output.WriteRawTag(10);
      output.WriteString(Symbol);
    }
    if (Price.Length != 0) {
      output.WriteRawTag(18);
      output.WriteString(Price);
    }
    if (Rate.Length != 0) {
      output.WriteRawTag(26);
      output.WriteString(Rate);
    }
    if (ZonedRate.Length != 0) {
      output.WriteRawTag(34);
      output.WriteString(ZonedRate);
    }
    if (High.Length != 0) {
      output.WriteRawTag(42);
      output.WriteString(High);
    }
    if (Low.Length != 0) {
      output.WriteRawTag(50);
      output.WriteString(Low);
    }
    if (Volume.Length != 0) {
      output.WriteRawTag(58);
      output.WriteString(Volume);
    }
    if (Quantity.Length != 0) {
      output.WriteRawTag(66);
      output.WriteString(Quantity);
    }
    if (LastCloseRate.Length != 0) {
      output.WriteRawTag(74);
      output.WriteString(LastCloseRate);
    }
    if (LastCloseZonedRate.Length != 0) {
      output.WriteRawTag(82);
      output.WriteString(LastCloseZonedRate);
    }
    if (LastCloseHigh.Length != 0) {
      output.WriteRawTag(90);
      output.WriteString(LastCloseHigh);
    }
    if (LastCloseLow.Length != 0) {
      output.WriteRawTag(98);
      output.WriteString(LastCloseLow);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
#endif
    }

#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output)
    {
        if (Symbol.Length != 0)
        {
            output.WriteRawTag(10);
            output.WriteString(Symbol);
        }
        if (Price.Length != 0)
        {
            output.WriteRawTag(18);
            output.WriteString(Price);
        }
        if (Rate.Length != 0)
        {
            output.WriteRawTag(26);
            output.WriteString(Rate);
        }
        if (ZonedRate.Length != 0)
        {
            output.WriteRawTag(34);
            output.WriteString(ZonedRate);
        }
        if (High.Length != 0)
        {
            output.WriteRawTag(42);
            output.WriteString(High);
        }
        if (Low.Length != 0)
        {
            output.WriteRawTag(50);
            output.WriteString(Low);
        }
        if (Volume.Length != 0)
        {
            output.WriteRawTag(58);
            output.WriteString(Volume);
        }
        if (Quantity.Length != 0)
        {
            output.WriteRawTag(66);
            output.WriteString(Quantity);
        }
        if (LastCloseRate.Length != 0)
        {
            output.WriteRawTag(74);
            output.WriteString(LastCloseRate);
        }
        if (LastCloseZonedRate.Length != 0)
        {
            output.WriteRawTag(82);
            output.WriteString(LastCloseZonedRate);
        }
        if (LastCloseHigh.Length != 0)
        {
            output.WriteRawTag(90);
            output.WriteString(LastCloseHigh);
        }
        if (LastCloseLow.Length != 0)
        {
            output.WriteRawTag(98);
            output.WriteString(LastCloseLow);
        }
        if (_unknownFields != null)
        {
            _unknownFields.WriteTo(ref output);
        }
    }
#endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize()
    {
        int size = 0;
        if (Symbol.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(Symbol);
        }
        if (Price.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(Price);
        }
        if (Rate.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(Rate);
        }
        if (ZonedRate.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(ZonedRate);
        }
        if (High.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(High);
        }
        if (Low.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(Low);
        }
        if (Volume.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(Volume);
        }
        if (Quantity.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(Quantity);
        }
        if (LastCloseRate.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(LastCloseRate);
        }
        if (LastCloseZonedRate.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(LastCloseZonedRate);
        }
        if (LastCloseHigh.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(LastCloseHigh);
        }
        if (LastCloseLow.Length != 0)
        {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(LastCloseLow);
        }
        if (_unknownFields != null)
        {
            size += _unknownFields.CalculateSize();
        }
        return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(PublicMiniTickerV3Api other)
    {
        if (other == null)
        {
            return;
        }
        if (other.Symbol.Length != 0)
        {
            Symbol = other.Symbol;
        }
        if (other.Price.Length != 0)
        {
            Price = other.Price;
        }
        if (other.Rate.Length != 0)
        {
            Rate = other.Rate;
        }
        if (other.ZonedRate.Length != 0)
        {
            ZonedRate = other.ZonedRate;
        }
        if (other.High.Length != 0)
        {
            High = other.High;
        }
        if (other.Low.Length != 0)
        {
            Low = other.Low;
        }
        if (other.Volume.Length != 0)
        {
            Volume = other.Volume;
        }
        if (other.Quantity.Length != 0)
        {
            Quantity = other.Quantity;
        }
        if (other.LastCloseRate.Length != 0)
        {
            LastCloseRate = other.LastCloseRate;
        }
        if (other.LastCloseZonedRate.Length != 0)
        {
            LastCloseZonedRate = other.LastCloseZonedRate;
        }
        if (other.LastCloseHigh.Length != 0)
        {
            LastCloseHigh = other.LastCloseHigh;
        }
        if (other.LastCloseLow.Length != 0)
        {
            LastCloseLow = other.LastCloseLow;
        }
        _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input)
    {
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
        input.ReadRawMessage(this);
#else
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
    if ((tag & 7) == 4) {
      // Abort on any end group tag.
      return;
    }
    switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 10: {
          Symbol = input.ReadString();
          break;
        }
        case 18: {
          Price = input.ReadString();
          break;
        }
        case 26: {
          Rate = input.ReadString();
          break;
        }
        case 34: {
          ZonedRate = input.ReadString();
          break;
        }
        case 42: {
          High = input.ReadString();
          break;
        }
        case 50: {
          Low = input.ReadString();
          break;
        }
        case 58: {
          Volume = input.ReadString();
          break;
        }
        case 66: {
          Quantity = input.ReadString();
          break;
        }
        case 74: {
          LastCloseRate = input.ReadString();
          break;
        }
        case 82: {
          LastCloseZonedRate = input.ReadString();
          break;
        }
        case 90: {
          LastCloseHigh = input.ReadString();
          break;
        }
        case 98: {
          LastCloseLow = input.ReadString();
          break;
        }
      }
    }
#endif
    }

#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input)
    {
        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            if ((tag & 7) == 4)
            {
                // Abort on any end group tag.
                return;
            }
            switch (tag)
            {
                default:
                    _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
                    break;
                case 10:
                    {
                        Symbol = input.ReadString();
                        break;
                    }
                case 18:
                    {
                        Price = input.ReadString();
                        break;
                    }
                case 26:
                    {
                        Rate = input.ReadString();
                        break;
                    }
                case 34:
                    {
                        ZonedRate = input.ReadString();
                        break;
                    }
                case 42:
                    {
                        High = input.ReadString();
                        break;
                    }
                case 50:
                    {
                        Low = input.ReadString();
                        break;
                    }
                case 58:
                    {
                        Volume = input.ReadString();
                        break;
                    }
                case 66:
                    {
                        Quantity = input.ReadString();
                        break;
                    }
                case 74:
                    {
                        LastCloseRate = input.ReadString();
                        break;
                    }
                case 82:
                    {
                        LastCloseZonedRate = input.ReadString();
                        break;
                    }
                case 90:
                    {
                        LastCloseHigh = input.ReadString();
                        break;
                    }
                case 98:
                    {
                        LastCloseLow = input.ReadString();
                        break;
                    }
            }
        }
    }
#endif

}

#endregion


#endregion Designer generated code
