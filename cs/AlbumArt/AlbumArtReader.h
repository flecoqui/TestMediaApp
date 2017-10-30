#pragma once

namespace AlbumArt
{
	public ref class AlbumArtReader sealed
	{
	public:
		AlbumArtReader();
		virtual ~AlbumArtReader();

		void Initialize(Windows::Storage::Streams::IRandomAccessStream^ stream);
		Windows::Foundation::IAsyncOperation<Windows::Storage::Streams::InMemoryRandomAccessStream^>^ GetAlbumArtAsync();

	private:
		void InitializeCommon();

	private:
		Microsoft::WRL::ComPtr<IMFSourceReader> m_pSourceReader;
	};

}
