namespace Shockah.UISuite;

public interface IUISuiteApi
{
	/// <summary>
	/// Registers a new hook related to the "browse cards in order" feature.
	/// </summary>
	/// <param name="hook">The hook.</param>
	/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c></param>
	void RegisterBrowseCardsInOrderHook(IBrowseCardsInOrderHook hook, double priority = 0);
			
	/// <summary>
	/// Unregisters the given hook related to the "browse cards in order" feature.
	/// </summary>
	/// <param name="hook">The hook.</param>
	void UnregisterBrowseCardsInOrderHook(IBrowseCardsInOrderHook hook);
	
	/// <summary>
	/// A hook related to the "browse cards in order" feature.
	/// </summary>
	public interface IBrowseCardsInOrderHook
	{
		/// <summary>
		/// Allows controlling whether the "Order" sort mode is enabled for the given <see cref="CardBrowse"/>.
		/// </summary>
		/// <param name="args">The arguments for the hook method.</param>
		/// <returns><c>true</c> if the "Order" sort mode should be enabled, <c>false</c> if not, <c>null</c> if the hook does not care.</returns>
		bool? ShouldAllowOrderSortModeInCardBrowse(IShouldAllowOrderSortModeInCardBrowseArgs args) => null;
		
		/// <summary>
		/// The arguments for the <see cref="ShouldAllowOrderSortModeInCardBrowse"/> hook method.
		/// </summary>
		public interface IShouldAllowOrderSortModeInCardBrowseArgs
		{
			/// <summary>
			/// The route the "Order" sort mode should be enabled for or not.
			/// </summary>
			CardBrowse Route { get; }
		}
	}
}