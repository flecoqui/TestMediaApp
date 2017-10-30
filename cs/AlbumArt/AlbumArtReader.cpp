#include "pch.h"
#include "AlbumArtReader.h"

using namespace AlbumArt;
using namespace Platform;
using namespace Microsoft::WRL;
inline void ThrowIfFailed(HRESULT hr, WCHAR const *szMessage = NULL)
{
	if (FAILED(hr))
	{
		// Set a breakpoint on this line to catch DirectX API errors
			throw std::exception();
	}
}
inline void TRACEMESSAGEA(char const *szMessage = NULL)
{
}
AlbumArtReader::AlbumArtReader()
{
	HRESULT hr = ::MFStartup(MF_VERSION);
	ThrowIfFailed(hr);
}

AlbumArtReader::~AlbumArtReader()
{
	HRESULT hr = ::MFShutdown();
	if (FAILED(hr)) TRACEMESSAGEA("MFShutdown failed");
}

void AlbumArtReader::Initialize(Windows::Storage::Streams::IRandomAccessStream^ stream)
{
	HRESULT hr;

	ComPtr<IUnknown> pStreamUnk = reinterpret_cast<IUnknown*>(stream);
	ComPtr<IMFByteStream> pMFStream;
	hr = ::MFCreateMFByteStreamOnStreamEx(pStreamUnk.Get(), &pMFStream);
	ThrowIfFailed(hr);

	hr = ::MFCreateSourceReaderFromByteStream(pMFStream.Get(), NULL, &m_pSourceReader);
	ThrowIfFailed(hr);
}

Windows::Foundation::IAsyncOperation<Windows::Storage::Streams::InMemoryRandomAccessStream^>^
AlbumArtReader::GetAlbumArtAsync()
{
	return concurrency::create_async([this]()
	{
		try
		{
			HRESULT hr;

			ComPtr<IMFMediaSource> pMediaSource;
			hr = m_pSourceReader->GetServiceForStream(MF_SOURCE_READER_MEDIASOURCE, GUID_NULL, IID_PPV_ARGS(&pMediaSource));
			ThrowIfFailed(hr);

			ComPtr<IMFGetService> pGetService;
			hr = pMediaSource.As(&pGetService);
			ThrowIfFailed(hr);

			ComPtr<IMFMetadataProvider> pMetadataProvider;
			hr = pGetService->GetService(MF_METADATA_PROVIDER_SERVICE, IID_PPV_ARGS(&pMetadataProvider));
			ThrowIfFailed(hr);

			ComPtr<IMFPresentationDescriptor> pPresentationDescriptor;
			hr = pMediaSource->CreatePresentationDescriptor(&pPresentationDescriptor);
			ThrowIfFailed(hr);

			ComPtr<IMFMetadata> pFileMetadata;
			hr = pMetadataProvider->GetMFMetadata(pPresentationDescriptor.Get(), 0, 0, &pFileMetadata);
			ThrowIfFailed(hr);

			PROPVARIANT propVar;
			PropVariantInit(&propVar);
			hr = pFileMetadata->GetProperty(L"WM/Picture", &propVar);

			if (SUCCEEDED(hr) && propVar.vt == VT_BLOB && propVar.blob.cbSize != 0)
			{
				BYTE *pData = propVar.blob.pBlobData;
				ULONG nData = propVar.blob.cbSize;

				// ASF_FLAT_PICTURE
				BYTE bPictureType = pData[0];
				pData += sizeof(BYTE);
				nData -= sizeof(BYTE);
				DWORD dwDataLen = 0;
				memcpy(&dwDataLen, pData, sizeof(DWORD));
				pData += sizeof(DWORD);
				nData -= sizeof(DWORD);

				// MIME type
				WCHAR *szBegin = (WCHAR*)pData;
				ULONG nsz = nData / sizeof(WCHAR);
				WCHAR *szEnd = std::find(szBegin, szBegin + nsz, L'\0');
				ThrowIfFailed(szEnd != szBegin + nsz, L"Failed to find mime type in blob data");

				std::wstring mimeType(szBegin, szEnd);
				nsz -= ((ULONG)mimeType.length() + 1);

				// description		
				szBegin = szEnd + 1;
				szEnd = std::find(szBegin, szBegin + nsz, L'\0');
				ThrowIfFailed(szEnd != szBegin + nsz, L"Failed to find description in blob data");

				std::wstring description(szBegin, szEnd);
				nsz -= ((ULONG)description.length() + 1);
				pData = (BYTE*)(szEnd + 1);
				nData = nsz * sizeof(WCHAR);

				// Image data
				Platform::ArrayReference<BYTE> dataRef(pData, nData);

				auto metaDataStream = ref new Windows::Storage::Streams::InMemoryRandomAccessStream();
				auto dataWriter = ref new Windows::Storage::Streams::DataWriter(metaDataStream->GetOutputStreamAt(0));
				dataWriter->WriteBytes(dataRef);

				return concurrency::create_task(dataWriter->StoreAsync())
					.then([dataWriter, metaDataStream](unsigned int bytesStored)
				{
					return dataWriter->FlushAsync();
				}).then([dataWriter, metaDataStream](bool flushOp)
				{
					dataWriter->DetachStream();
					return metaDataStream;
				});
			}
		}
		catch (Platform::Exception^ ex)
		{
			TRACEMESSAGEA("HR exception caught in AlbumArtReader::GetAlbumArtAsync(): %s\n"/*, ex->ToString()->Data()*/);
		}
		catch (...)
		{
			TRACEMESSAGEA("unknown exception caught in AlbumArtReader::GetAlbumArtAsync()\n");
		}

		return concurrency::create_task([]()->Windows::Storage::Streams::InMemoryRandomAccessStream^ { return nullptr; });
	});
}

