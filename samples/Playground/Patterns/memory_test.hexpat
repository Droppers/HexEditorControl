#pragma array_limit 0xFFFFFF
#pragma pattern_limit 0xFFFFFF

struct ContentContainerInformation {
	u32 index;
	u32 hash;
	u32 size;
} [[static]];

struct ContentBlockInformation {
	u32 hash;
	u32 directory_index;
	u32 file_index;
	u32 content_container_index;
	u32 file_offset;
	u32 compressed_size;
	u32 uncompressed_size;
} [[static]];

struct MetaContainer {
	u32 version;
	u32 content_container_information_count;
	ContentContainerInformation content_container_informations[content_container_information_count];
	u32 content_block_information_count;
	ContentBlockInformation content_block_informations[content_block_information_count];
};

MetaContainer meta_container @ 0x00;