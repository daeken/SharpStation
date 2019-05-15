#pragma warning disable 169
namespace SharpStation {
	public class SPU {
		[Port(0x1F801D80)] static ushort MainVolumeLeft;
		[Port(0x1F801D82)] static ushort MainVolumeRight;
		[Port(0x1F801D84)] static ushort ReverbOutputVolumeLeft;
		[Port(0x1F801D86)] static ushort ReverbOutputVolumeRight;

		[Port(0x1F801D88)] static ushort KeyOnLo; // Supposed to be write-only but read by BIOS??
		[Port(0x1F801D8A)] static ushort KeyOnHi;
		
		[Port(0x1F801D8C)] static ushort KeyOffLo; // Supposed to be write-only but read by BIOS??
		[Port(0x1F801D8E)] static ushort KeyOffHi;

		[Port(0x1F801D90)] static void ChannelFMModeLo(ushort v) {}
		[Port(0x1F801D92)] static void ChannelFMModeHi(ushort v) {}

		[Port(0x1F801D94)] static void ChannelNoiseModeLo(ushort v) {}
		[Port(0x1F801D96)] static void ChannelNoiseModeHi(ushort v) {}

		[Port(0x1F801D98)] static void ChannelReverbModeLo(ushort v) {}
		[Port(0x1F801D9A)] static void ChannelReverbModeHi(ushort v) {}

		[Port(0x1F801DA2)] static ushort RamReverbWorkAreaStartAddress;
		[Port(0x1F801DA4)] static ushort RamIrqAddress;
		[Port(0x1F801DA6)] static ushort RamDataTransferAddress;
		[Port(0x1F801DA8)] static ushort RamDataTransferFifo;
		[Port(0x1F801DAA)] static ushort Control;
		[Port(0x1F801DAC)] static ushort RamDataTransferControl;
		[Port(0x1F801DAE)] static ushort Status => 0;
		
		[Port(0x1F801DB0)] static ushort CdVolumeLeft;
		[Port(0x1F801DB2)] static ushort CdVolumeRight;
		[Port(0x1F801DB4)] static ushort ExternVolumeLeft;
		[Port(0x1F801DB6)] static ushort ExternVolumeRight;

		[Port(0x1F801C00, 24, 0x10)] static ushort[] VoiceVolumeLeft;
		[Port(0x1F801C02, 24, 0x10)] static ushort[] VoiceVolumeRight;
		[Port(0x1F801C04, 24, 0x10)] static ushort[] VoiceSampleRate;
		[Port(0x1F801C06, 24, 0x10)] static ushort[] VoiceStartAddress;
		[Port(0x1F801C08, 24, 0x10)] static ushort[] VoiceAdsrLo;
		[Port(0x1F801C0A, 24, 0x10)] static ushort[] VoiceAdsrHi;
	}
}