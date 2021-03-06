/*
This file is part of PacketDotNet

PacketDotNet is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

PacketDotNet is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with PacketDotNet.  If not, see <http://www.gnu.org/licenses/>.
*/
/*
 * Copyright 2012 Alan Rushforth <alan.rushforth@gmail.com>
 */

using System.Net.NetworkInformation;
using PacketDotNet.Utils;
using PacketDotNet.Utils.Converters;

namespace PacketDotNet.Ieee80211
{
    /// <summary>
    /// Block acknowledgment request frame.
    /// </summary>
    public sealed class BlockAcknowledgmentRequestFrame : MacFrame
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="byteArraySegment">
        /// A <see cref="ByteArraySegment" />
        /// </param>
        public BlockAcknowledgmentRequestFrame(ByteArraySegment byteArraySegment)
        {
            Header = new ByteArraySegment(byteArraySegment);

            FrameControl = new FrameControlField(FrameControlBytes);
            Duration = new DurationField(DurationBytes);
            ReceiverAddress = GetAddress(0);
            TransmitterAddress = GetAddress(1);
            BlockAcknowledgmentControl = new BlockAcknowledgmentControlField(BlockAckRequestControlBytes);
            BlockAckStartingSequenceControl = BlockAckStartingSequenceControlBytes;

            Header.Length = FrameSize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockAcknowledgmentRequestFrame" /> class.
        /// </summary>
        /// <param name='transmitterAddress'>
        /// Transmitter address.
        /// </param>
        /// <param name='receiverAddress'>
        /// Receiver address.
        /// </param>
        public BlockAcknowledgmentRequestFrame
        (
            PhysicalAddress transmitterAddress,
            PhysicalAddress receiverAddress)
        {
            FrameControl = new FrameControlField();
            Duration = new DurationField();
            ReceiverAddress = receiverAddress;
            TransmitterAddress = transmitterAddress;
            BlockAcknowledgmentControl = new BlockAcknowledgmentControlField();

            FrameControl.SubType = FrameControlField.FrameSubTypes.ControlBlockAcknowledgmentRequest;
        }

        /// <summary>
        /// Block acknowledgment control field
        /// </summary>
        public BlockAcknowledgmentControlField BlockAcknowledgmentControl { get; set; }

        /// <summary>
        /// Gets or sets the sequence number of the first MSDU for which this
        /// block acknowledgement request frame is sent
        /// </summary>
        /// <value>
        /// The block ack starting sequence control field value
        /// </value>
        public ushort BlockAckStartingSequenceControl { get; set; }

        /// <summary>
        /// Length of the frame
        /// </summary>
        public override int FrameSize => MacFields.FrameControlLength +
                                         MacFields.DurationIDLength +
                                         (MacFields.AddressLength * 2) +
                                         BlockAcknowledgmentRequestFields.BlockAckRequestControlLength +
                                         BlockAcknowledgmentRequestFields.BlockAckStartingSequenceControlLength;

        /// <summary>
        /// Receiver address
        /// </summary>
        public PhysicalAddress ReceiverAddress { get; set; }

        /// <summary>
        /// Transmitter address
        /// </summary>
        public PhysicalAddress TransmitterAddress { get; set; }

        /// <summary>
        /// Block acknowledgment control bytes are the first two bytes of the frame
        /// </summary>
        private ushort BlockAckRequestControlBytes
        {
            get
            {
                if (Header.Length >=
                    BlockAcknowledgmentRequestFields.BlockAckRequestControlPosition +
                    BlockAcknowledgmentRequestFields.BlockAckRequestControlLength)
                {
                    return EndianBitConverter.Little.ToUInt16(Header.Bytes,
                                                              Header.Offset + BlockAcknowledgmentRequestFields.BlockAckRequestControlPosition);
                }

                return 0;
            }
            set => EndianBitConverter.Little.CopyBytes(value,
                                                       Header.Bytes,
                                                       Header.Offset + BlockAcknowledgmentRequestFields.BlockAckRequestControlPosition);
        }

        /// <summary>
        /// Gets or sets the block ack starting sequence control.
        /// </summary>
        /// <value>
        /// The block ack starting sequence control.
        /// </value>
        private ushort BlockAckStartingSequenceControlBytes
        {
            get
            {
                if (Header.Length >=
                    BlockAcknowledgmentRequestFields.BlockAckStartingSequenceControlPosition +
                    BlockAcknowledgmentRequestFields.BlockAckStartingSequenceControlLength)
                {
                    return EndianBitConverter.Little.ToUInt16(Header.Bytes,
                                                              Header.Offset + BlockAcknowledgmentRequestFields.BlockAckStartingSequenceControlPosition);
                }

                return 0;
            }
            set => EndianBitConverter.Little.CopyBytes(value,
                                                       Header.Bytes,
                                                       Header.Offset + BlockAcknowledgmentRequestFields.BlockAckStartingSequenceControlPosition);
        }

        /// <summary>
        /// Writes the current packet properties to the backing ByteArraySegment.
        /// </summary>
        public override void UpdateCalculatedValues()
        {
            if (Header == null || Header.Length > Header.BytesLength - Header.Offset || Header.Length < FrameSize)
            {
                Header = new ByteArraySegment(new byte[FrameSize]);
            }

            FrameControlBytes = FrameControl.Field;
            DurationBytes = Duration.Field;
            SetAddress(0, ReceiverAddress);
            SetAddress(1, TransmitterAddress);

            BlockAckRequestControlBytes = BlockAcknowledgmentControl.Field;
            BlockAckStartingSequenceControlBytes = BlockAckStartingSequenceControl;

            Header.Length = FrameSize;
        }

        /// <summary>
        /// Returns a string with a description of the addresses used in the packet.
        /// This is used as a component of the string returned by ToString().
        /// </summary>
        /// <returns>
        /// The address string.
        /// </returns>
        protected override string GetAddressString()
        {
            return $"RA {ReceiverAddress} TA {TransmitterAddress}";
        }
    }
}