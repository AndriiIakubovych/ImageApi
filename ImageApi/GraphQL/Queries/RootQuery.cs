namespace ImageApi.GraphQL.Queries
{
	public class RootQuery
	{
		public ImageQuery Image => new ImageQuery();
		public ThumbnailJobQuery Thumbnail => new ThumbnailJobQuery();
	}
}
