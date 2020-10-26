using System;
using System.Collections.Generic;
using Jellyfin.Plugin.IMVDb.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.IMVDb
{
    /// <summary>
    /// IMVDb Plugin.
    /// </summary>
    public class ImvdbPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Gets the provider id.
        /// </summary>
        public const string ProviderName = "IMVDb";

        /// <summary>
        /// Initializes a new instance of the <see cref="ImvdbPlugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public ImvdbPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets current plugin instance.
        /// </summary>
        public static ImvdbPlugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => "IMVDb";

        /// <inheritdoc />
        public override Guid Id => new Guid("A4967B35-15B3-46F0-BC7E-0B7D90623A85");

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.config.html"
            };
        }
    }
}