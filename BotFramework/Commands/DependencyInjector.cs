using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BotFramework.Bot;
using BotFramework.Commands.Validators;
using FastExpressionCompiler;
using Optional;
using Optional.Collections;
using Telegram.Bot.Types;
using static System.Linq.Expressions.Expression;

namespace BotFramework.Commands
{
    public class DependencyInjector
    {
        //Dictionary <T, Validator<T>>
        public DependencyInjector(Dictionary<Type, Type> validators) => this.validators = validators;

        private Dictionary<Type, Type> validators { get; }

        private Dictionary<Type, Func<Update, IGetOnlyClient, Option<ICommand>>> commandCreators { get; } =
            new Dictionary<Type, Func<Update, IGetOnlyClient, Option<ICommand>>>();

        public IEnumerable<ICommand> GetPossible(IEnumerable<Type> commandTypes, Update tgUpdate,
                                                 IGetOnlyClient    client)
        {
            return commandTypes.Where(t => t.GetInterfaces().Contains(typeof(ICommand)) && !t.IsAbstract)
                               .Select(t => Create(tgUpdate, client, t))
                               .Select(t =>
                               t.FlatMap(k => (k as ICommand).SomeNotNull()))
                               .Values();
        }

        public IEnumerable<ICommand> GetPossibleCompiler(IEnumerable<Type> commandTypes, Update tgUpdate,
                                                         IGetOnlyClient    client)
        {
            return commandTypes.Where(t => t.GetInterfaces().Contains(typeof(ICommand)) && !t.IsAbstract)
                               .Select(t =>
                               {
                                   if (!commandCreators.ContainsKey(t))
                                       commandCreators[t] = CompileCommand(t);
                                   return commandCreators[t](tgUpdate, client);
                               })
                               .Values();
        }

        private Option<object> Create(Update tgUpdate, IGetOnlyClient client, Type objType)
        {
            if (objType == typeof(Update)) return ((object) tgUpdate).Some();
            if (objType == typeof(IGetOnlyClient)) return ((object) client).Some();
            if (validators.ContainsKey(objType))
                return Create(tgUpdate, client, validators[objType])
                .FlatMap(validator => ((Validator) validator).Validate());

            if (objType.GetInterfaces().Contains(typeof(Validator)) || objType.GetInterfaces().Contains(typeof(ICommand)))
                foreach (var constructor in objType.GetConstructors())
                {
                    var paramTypes = constructor.GetParameters().Select(t => t.ParameterType).ToArray();
                    var parameters = paramTypes.Select(t => Create(tgUpdate, client, t)).Values().ToArray();
                    if (parameters.Length != paramTypes.Length)
                        continue;
                    return Activator.CreateInstance(objType, parameters).SomeNotNull();
                }

            return Option.None<object>();
        }

        public Func<Update, IGetOnlyClient, Option<ICommand>> CompileCommand(Type commandType)
        {
            var vars = new Dictionary<Type, ParameterExpression>
            {
                [typeof(IGetOnlyClient)] = Parameter(typeof(IGetOnlyClient), "client"),
                [typeof(Update)]         = Parameter(typeof(Update),         "update")
            };

            var body = CreateExpr(SortDependencies(commandType, new Stack<Type>(), new HashSet<Type>()), vars);
            var func = Lambda<Func<Update, IGetOnlyClient, Option<ICommand>>>(body: body,
                parameters: new[] {vars[typeof(Update)], vars[typeof(IGetOnlyClient)]});
            return func.CompileFast();
        }

        private Expression CreateExpr(Stack<Type> types, Dictionary<Type, ParameterExpression> variables)
        {
            var currentType = types.Pop();


            IEnumerable<ParameterExpression> GetParams(ConstructorInfo constructor) =>
            constructor.GetParameters()
                       .Select(t => t.ParameterType)
                       .Select(t =>
                       {
                           if (t == typeof(Update))
                               return variables[typeof(Update)];
                           if (t == typeof(IGetOnlyClient))
                               return variables[typeof(IGetOnlyClient)];
                           if (!variables.ContainsKey(t))
                               variables[t] = Parameter(t);
                           return variables[t];
                       });

            if (types.Count == 0)
            {
                var constructor = currentType.GetConstructors()[0];
                var asType      = TypeAs(New(constructor, GetParams(constructor)), typeof(ICommand));
                return Call(typeof(Option), "Some", new[] {typeof(ICommand)}, asType);
            }

            var validatorConstructor = validators[currentType].GetConstructors()[0];
            var callValidate = Call(New(validatorConstructor, GetParams(validatorConstructor)),
                validators[currentType].GetMethod("Validate")!);
            if (!variables.ContainsKey(currentType)) variables[currentType] = Parameter(currentType);
            var func = Lambda(CreateExpr(types, variables), variables[currentType]);
            var method = typeof(Option<>).MakeGenericType(currentType)
                                         .GetMethods()
                                         .Where(t => t.Name                         == "FlatMap")
                                         .Where(t => t.GetGenericArguments().Length == 1)
                                         .Select(t => t.MakeGenericMethod(func.ReturnType.GenericTypeArguments[0]))
                                         .First();
            var callFlatMap = Call(callValidate, method, func);
            return callFlatMap;
        }

        private Stack<Type> SortDependencies(Type currentType, Stack<Type> res, HashSet<Type> helper)
        {
            if (currentType == typeof(IGetOnlyClient) || currentType == typeof(Update)) return res;
            if (!helper.Contains(currentType))
            {
                res.Push(currentType);
                helper.Add(currentType);
            }

            foreach (var par in currentType.GetConstructors().Select(t => t.GetParameters()).First())
                SortDependencies(par.ParameterType, res, helper);
            return res;
        }

        //todo test
        public static void CopyAllParams(object newObject, object parentObject)
        {
            var type     = newObject.GetType();
            var baseType = parentObject.GetType();
            if (type.BaseType != baseType)
                throw new InvalidCastException($"{type} is not child of {baseType}");
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Default;
            foreach (var field in baseType.GetFields(flags))
                field.SetValue(newObject, field.GetValue(parentObject));
            foreach (var property in baseType.GetProperties().Where(t => t.SetMethod != null && t.GetMethod != null))
                property.SetValue(newObject, property.GetValue(parentObject));
        }
    }
}