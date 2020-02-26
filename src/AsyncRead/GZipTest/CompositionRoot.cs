using GZipTest.Arch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest
{
    internal sealed class CompositionRoot
    {
        private readonly ServiceCollection _root;
        private readonly ServiceProvider _provider;

        public static CompositionRoot Current { get; }

        static CompositionRoot() {
            Current = new CompositionRoot();
        }

        private CompositionRoot() {
            _root = new ServiceCollection();
            _root.AddLogging();

            _root.AddSingleton<Archiver>(provider => {
                var logger = provider.GetService<ILogger<ArchiverWorker>>();
                Func<string, string, ArchSettings, int, ArchiverWorker> factory =
                (target, arch, settings, jobs) =>
                    new ArchiverWorker(target, arch, settings, jobs, logger);
                return new Archiver(factory);
            });
            _root.AddTransient<ArchiverWorker>();

            _provider = _root.BuildServiceProvider();
        }

        public Archiver GetArchiver() {
            return _provider.GetService<Archiver>();
        }
    }
}