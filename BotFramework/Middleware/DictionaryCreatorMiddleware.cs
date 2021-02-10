using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BotFramework.Abstractions;
using BotFramework.Helpers;
using Telegram.Bot.Types;

namespace BotFramework.Middleware
{
    public class DictionaryContext
    {
        /// <summary>
        ///     First not done handler will handle CurrentUpdate
        /// </summary>
        public LinkedList<IUpdateConsumer> Handlers { get; set; } = new();
    }

    public class DictionaryCreatorMiddleware
    {
        private readonly ConcurrentDictionary<long, LinkedList<IUpdateConsumer>> dictionary = new();
        private readonly UpdateDelegate                                          _next;

        public DictionaryCreatorMiddleware(UpdateDelegate next)
        {
            _next = next;
        }

        public Task Invoke(Update update, DictionaryContext dictionaryContext)
        {
            if (update.GetId() is not { } id)
            {
                return _next.Invoke(update);
            }

            dictionary.AddOrUpdate(
                id, _ => new LinkedList<IUpdateConsumer>(),
                (_, list) => new LinkedList<IUpdateConsumer>(list.Where(t => !t.IsDone)));
            dictionaryContext.Handlers = dictionary[id];

            return _next.Invoke(update);
        }
    }
}