using Raven.Client.Documents;
using System;

namespace YourClassLibrary.RavenDB
{
    public static class DocumentStoreHolder
    {
        private static readonly Lazy<IDocumentStore> LazyStore =
            new Lazy<IDocumentStore>(() => CreateStore());

        public static IDocumentStore Store => LazyStore.Value;

        private static IDocumentStore CreateStore()
        {
            IDocumentStore store = new DocumentStore
            {
                // Replace with your RavenDB server URL(s)
                Urls = new[] { "http://localhost:8080" },
                // Replace with your database name
                Database = "GjammT"
            };

            // Optional: Configure conventions, etc.
            // store.Conventions...

            store.Initialize(); // Crucial: Initializes the DocumentStore

            return store;
        }

        // Optional: Add a method to explicitly dispose of the store if needed,
        // though for application lifetime, it's often managed by the app's lifecycle.
        public static void DisposeStore()
        {
            if (LazyStore.IsValueCreated)
            {
                LazyStore.Value.Dispose();
            }
        }
    }
}