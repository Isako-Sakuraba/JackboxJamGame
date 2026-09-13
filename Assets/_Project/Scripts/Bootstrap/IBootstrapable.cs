namespace Game.Lifecycle
{
    /// <summary>
    /// Performs service or scene setup during bootstrap.
    /// </summary>
    public interface IBootstrapable
    {
        /// <summary>
        /// Initializes the object during startup bootstrap.
        /// </summary>
        public void Bootstrap();
    }
}
