using System;
using System.Collections.Generic;
using System.Text;

namespace HA.EhZ;

//<summary>
// http://wiki.volkszaehler.org/hardware/channels/meters/power/edl-ehz/emh-ehz-h1
//</summary>
public class SmlParser
{
    public class SmlItem
    {
        public SmlItem Parent { get; set; }

        public string Code { get; set; }

        public List<(int, string, byte[])> Values { get; private set; } = new List<(int, string, byte[])>();

        public List<(int, SmlItem)> List { get; private set; } = new List<(int, SmlItem)>();

        public override string ToString()
        {
            return $"Parent Code: {Parent?.Code ?? ""} Code: {Code} Count: Values {Values.Count} List {List.Count}";
        }
    }


    public interface ISmlItem
    {
        public bool IsListItem { get; }
    }


    public struct SmlList : ISmlItem
    {
        public SmlList() { }

        public bool IsListItem => true;

        public List<ISmlItem> Children { get; set; } = new List<ISmlItem>();

        public int ListLength { get; set; } = 0;

        public List<byte[]> Entries { get; private set; } = new List<byte[]>();
    }

    public struct SmlEntry : ISmlItem
    {
        public SmlEntry() { }

        public bool IsListItem => false;

        public List<ISmlItem> Children { get; set; } = new List<ISmlItem>();

        public int ListLength { get; set; } = 0;

        public List<byte[]> Entries { get; private set; } = new List<byte[]>();
    }

    public struct SmlMessage
    {
        public string TransactionId { get; set; }

        public string GroupNo { get; set; }

        public string AbourtOnError { get; set; }

        public int ListLength { get; set; }

        public bool BodyContainsList => ListLength > 0;

        public string Body { get; set; }

        public string CRC { get; set; }
    }

    public struct ParserItem
    {
        public byte Value { get; set; }

        public int Start { get; set; }

        public byte Count { get; set; }

        public bool List { get; set; }
    }

    // [2023-08-31 08:44:31,102][sml.COM4] WARNING  | No configuration found for 080c2aed2d4c693b
    // [2023 - 08 - 31 08:44:31, 102][sml.COM4] INFO     | Creating default value handler for 0100 0108 00ff
    // [2023 - 08 - 31 08:44:31, 102][sml.COM4] INFO     | Creating default value handler for 0100 0108 01ff
    // [2023 - 08 - 31 08:44:31, 102][sml.COM4] INFO     | Creating default value handler for 0100 0208 00ff
    // [2023 - 08 - 31 08:44:31, 102][sml.COM4] INFO     | Creating default value handler for 0100 0208 01ff
    // [2023 - 08 - 31 08:44:31, 104][sml.COM4] INFO     | Creating default value handler for 0100 1007 00ff
    // [2023 - 08 - 31 08:44:31, 105][sml.mqtt.pub] INFO     | sml2mqtt/080c2aed2d4c693b/0100 0108 00ff: 25483.5075 (QOS: 0, retain: False)
    // [2023-08-31 08:44:31,105][sml.mqtt.pub] INFO          | sml2mqtt/080c2aed2d4c693b/0100 0208 00ff: 61456.9708 (QOS: 0, retain: False)
    // [2023-08-31 08:44:31,106][sml.mqtt.pub] INFO          | sml2mqtt/080c2aed2d4c693b/0100 0108 01ff: 25483.5075 (QOS: 0, retain: False)
    // [2023-08-31 08:44:31,106][sml.mqtt.pub] INFO          | sml2mqtt/080c2aed2d4c693b/0100 0208 01ff: 61456.9708 (QOS: 0, retain: False)
    // [2023-08-31 08:44:31,106][sml.mqtt.pub] INFO          | sml2mqtt/080c2aed2d4c693b/0100 1007 00ff: -2378.9 (QOS: 0, retain: False)

    private static readonly byte[] startSequence = { 0x1B, 0x1B, 0x1B, 0x1B, 0x01, 0x01, 0x01, 0x01 };
    private static readonly byte[] stopSequence = { 0x1B, 0x1B, 0x1B, 0x1B };
    private static readonly byte[] powerSequence = { 0x77, 0x07, 0x01, 0x00, 0x10, 0x07, 0x00, 0xFF }; //sequence preceeding the current "pos Wirkenergie, 1.0.8." value (4 Bytes)

    private static readonly byte[] c_SeqConsumedEnergy1 = new byte[] { 0x77, 0x07, 0x01, 0x00, 0x01, 0x08, 0x00, 0xFF };
    private static readonly byte[] c_SeqProducedEnergy1 = new byte[] { 0x77, 0x07, 0x01, 0x00, 0x02, 0x08, 0x00, 0xFF };
    private static readonly byte[] c_SeqConsumedEnergy2 = new byte[] { 0x77, 0x07, 0x01, 0x00, 0x01, 0x08, 0x01, 0xFF };
    private static readonly byte[] c_SeqProducedEnergy2 = new byte[] { 0x77, 0x07, 0x01, 0x00, 0x02, 0x08, 0x01, 0xFF };

    private readonly List<byte> _data = new List<byte>();
    private int _currentStart = -1;
    private DateTime _measuredUtcTime = DateTime.MinValue;

    public DateTime MeasuredUtcTime { get; set; }

    public int MinMessageBytes { get; set; } = 299;

    public EhZMeasurement AddBytes(byte[] data)
    {
        _data.AddRange(data);
        if (_data.Count < MinMessageBytes)
        {
            return null;
        }
        var dataArray = _data.ToArray();
        if (_currentStart < 0)
        {
            var seqStart = SearchStart(dataArray);
            if (seqStart.seqStart == -1)
            {
                return null;
            }
            else if (seqStart.seqStart > 0)
            {
                _data.RemoveRange(0, seqStart.seqStart);
            }
            if (_data.Count < MinMessageBytes)
                return null;
            var endPos = SearchSequence(stopSequence, dataArray, 8);
            if (endPos.seqStart == -1)
            {
                return null;
            }
            _measuredUtcTime = DateTime.UtcNow;
            _currentStart = 8;
            //var message = BitConverter.ToString(dataArray).Replace("-", " ");

            //var header = ParseSmlMessage(dataArray, ref _currentStart);
            //var test = ParseSmlMessage(dataArray, ref _currentStart);

            //var listCount = CheckList(dataArray, _currentStart);


            //var itemCount = CheckList(dataArray, _currentStart + 1);
            //_currentStart = 8;
        }
        var measurement = CreateEhZMeasurement(dataArray);
        _data.Clear();
        _currentStart = -1;
        _measuredUtcTime = DateTime.MinValue;
        return measurement;
    }

    private SmlItem ParseSmlMessage(byte[] dataArray, ref int pos, SmlItem parent = null)
    {
        var smlItem = new SmlItem
        {
            Code = BitConverter.ToString(dataArray, pos, 1),
            Parent = parent
        };
        var listLength = GetListLength(dataArray[pos++]);
        //var message = new SmlMessage { ListLength = GetListLength(dataArray[pos++]) };
        //var listLength = GetListLength(dataArray[pos++]);
        if (listLength > 0)
        {
            var stringList = new List<string>();
            for (var x = 0; x < listLength; x++)
            {
                if (IsListEntry(dataArray[pos]))
                {
                    // List
                    var smlItemCild = ParseSmlMessage(dataArray, ref pos, smlItem);
                    smlItemCild.Parent = smlItem;
                    smlItem.List.Add((x, smlItemCild));
                }
                else
                {
                    // Item
                    var array = CheckEntry(dataArray, ref pos);
                    stringList.Add(array.Item1);
                    smlItem.Values.Add((x, array.Item1, array.Item2));
                }
            }
        }
        if (smlItem.Parent == null)
        {
            var count = 0;
            while (dataArray[pos] != 0)
            {
                pos++;
                count++;
            }
            pos++;
        }
        return smlItem;
    }

    private bool IsListEntry(byte data)
    {
        return (data & 0xF0) == 0x70;
    }

    private int GetListLength(byte data)
    {
        if ((data & 0xF0) == 0x70)
            return (byte)(data & 0x0F);
        return 0;
    }

    private (string, byte[]) CheckEntry(byte[] data, ref int pos)
    {
        var entry = data[pos];
        var length = entry & 0x0F;
        if ((entry & 0xF0) == 0x80)
        {
            length = 16 * length + data[pos + 1];
        }
        var array = new byte[length];
        var off = 0;
        Buffer.BlockCopy(data, pos, array, off, length);
        //for (var x = pos; off < length; x++, off++) 
        //    array[off] = data[x];
        string check = BitConverter.ToString(array);
        pos += length;
        return (check, array);
    }


    private ParserItem CheckList(byte[] data, int pos)
    {
        var value = data[pos];
        if ((value & 0xF0) == 0x70)
        {
            return new ParserItem { Start = pos, Count = (byte)(value & 0x0F), List = true, Value = value };
        }
        return new ParserItem { Start = pos, Count = (byte)(value & 0x0F), List = false, Value = value };
    }

    private byte CheckEntry(byte value)
    {
        if ((value & 0xF0) == 0x70)
        {
            return (byte)(value & 0x0F);
        }
        return 0;
    }

    private EhZMeasurement CreateEhZMeasurement(byte[] dataArray)
    {
        var dataString = BitConverter.ToString(dataArray);
        Console.WriteLine(dataString);

        var consumeEnergy1 = SearchSequenceAndParse(
            dataArray,
            _currentStart,
            c_SeqConsumedEnergy1,
            10);
        var producedEnergy1 = SearchSequenceAndParse(
            dataArray,
            _currentStart,
            c_SeqProducedEnergy1,
            10);
        var consumeEnergy2 = SearchSequenceAndParse(
            dataArray,
            _currentStart,
            c_SeqConsumedEnergy2,
            7);
        var producedEnergy2 = SearchSequenceAndParse(
            dataArray,
            _currentStart,
            c_SeqProducedEnergy2,
            7);
        var powerConsumption = SearchSequenceAndParse(
            dataArray,
            _currentStart,
            powerSequence,
            7, 4, 10.0);

        byte[] sequence = { 0x77, 0x07 };
        var pos = _currentStart;
        var sb = new StringBuilder();
        while (pos > 0 && pos < 232 - 21)
        {
            var result = SearchSequence(sequence, dataArray, pos);
            var tempPos = result.seqStart;
            if (tempPos < 0)
                break;
            var resultArray = ArrayHelpers.ReadAndCreateArrayFrom(
                dataArray, ref tempPos, result.seqEnd - result.seqStart + 21);
            var sequenceString = BitConverter.ToString(resultArray);
            sb.AppendLine(sequenceString);
            pos = result.seqEnd + 11;
        }
        var t = sb.ToString();

        return new EhZMeasurement
        {
            MeasuredUtcTime = _measuredUtcTime,
            ConsumedEnergy1 = consumeEnergy1.HasValue ? consumeEnergy1.Value : 0,
            ConsumedEnergy2 = consumeEnergy2.HasValue ? consumeEnergy2.Value : 0,
            ProducedEnergy1 = producedEnergy1.HasValue ? producedEnergy1.Value : 0,
            ProducedEnergy2 = producedEnergy2.HasValue ? producedEnergy2.Value : 0,
            CurrentPower = powerConsumption.HasValue ? powerConsumption.Value : 0
        };
    }

    private double? SearchSequenceAndParse(byte[] data, int startIndex, byte[] sequence, int offset, int numberOfBytes = 5, double factor = 10000.0)
    {
        var pos = SearchSequence(sequence, data, startIndex);
        if (pos.seqStart >= 0)
        {
            var index = pos.seqEnd += offset;
            var result = ArrayHelpers.ReadAndCreateArrayFrom(data, ref index, numberOfBytes);
            return ArrayHelpers.ConvertTo(result) / factor;
        }
        return new double?();
    }

    //<summary>
    // 1B 1B 1B 1B   Start Escape Zeichenfolge
    // 01 01 01 01   Start Übertragung Version 1
    // 76            Liste mit 6 Einträgen
    //</summary>
    private (int seqStart, int seqEnd) SearchStart(byte[] data, int startIndex = 0)
    {
        // { 0x1B, 0x1B, 0x1B, 0x1B, 0x01, 0x01, 0x01, 0x01 }
        const byte b1b = 0x1B;
        const byte b01 = 0x01;
        var stopIndex = data.Length - 8;
        for (var x = startIndex; x < stopIndex; x++)
        {
            if (data[x] == b1b && data[x + 1] == b1b &&
                data[x + 2] == b1b && data[x + 3] == b1b &&
                data[x + 4] == b01 && data[x + 5] == b01 &&
                data[x + 6] == b01 && data[x + 7] == b01)
            {
                return (seqStart: x, seqEnd: x + 8);
            }
        }
        return (seqStart: -1, seqEnd: stopIndex);
    }

    private (int seqStart, int seqEnd) SearchSequence(byte[] sequence, byte[] data, int startIndex)
    {
        var sequenceLength = sequence.Length;
        for (var x = startIndex; x < data.Length - sequenceLength; x++)
        {
            var i = x;
            for (var s = 0; s < sequenceLength; s++)
            {
                if (data[i++] != sequence[s])
                {
                    break;
                }
                else if (s < sequenceLength - 1)
                {
                    continue;
                }
                return (seqStart: x, seqEnd: x + sequenceLength);
            }
        }
        return (seqStart: -1, seqEnd: -1);
    }
}