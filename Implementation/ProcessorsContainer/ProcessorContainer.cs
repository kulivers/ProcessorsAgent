﻿using System.Collections;
using System.Reflection;
using ElasticClient;
using Newtonsoft.Json;
using Processor;
using Processor.Api;
using Processor.Api.Exceptions;
using ProcessorsRunner;

public class ProcessorContainer : IProcessorsContainer
{
    public List<IProcessor?> Processors { get; }

    public ProcessorContainer(ProcessorsConfigs processorsConfigs)
    {
        Processors = new List<IProcessor?>();
        foreach (var config in processorsConfigs.Processors)
        {
            var assembly = Assembly.LoadFrom(config.Dll);
            var assTypes = assembly.GetTypes();
            var factories = FindFactories(assTypes);
            foreach (var factoryType in factories)
            {
                var factoryInstance = Activator.CreateInstance(factoryType);
                var factoryMethodName = typeof(IProcessorFactory<,>).GetMethods().First().Name;
                var factoryMethod = factoryType.GetMethod(factoryMethodName);
                var serviceInstance = factoryMethod?.Invoke(factoryInstance, new object[] { config });
                if (serviceInstance is IProcessor processor)
                {
                    AddProcessor(processor);
                }
                else
                {
                    throw new CantLoadServiceException(serviceInstance?.ToString() ?? "Unknown");
                }
            }
        }
    }

    private IEnumerable<Type> FindFactories(IEnumerable<Type> types)
    {
        var typesWithAttribute = types.Where(it =>
            it.GetCustomAttributes().Any(attribute => attribute.GetType() == typeof(ProcessElementAttribute)));
        foreach (var type in typesWithAttribute)
        {
            foreach (var attribute in type.GetCustomAttributes())
            {
                if (attribute is ProcessElementAttribute { Type: ProcessingAttributeBehaviourType.Factory })
                {
                    yield return type;
                }
            }
        }
    }

    public void AddProcessor(IProcessor? processor)
    {
        Processors.Add(processor);
    }

    public IProcessor<TIn, TOut>? GetProcessor<TIn, TOut>()
    {
        IProcessor<TIn, TOut>? toReturn = null;
        foreach (var processor in Processors)
        {
            if (processor is IProcessor<TIn, TOut> other)
                toReturn = other;
        }

        return toReturn;
    }

    public IProcessor? GetProcessor(string serviceName)
    {
        return Processors.FirstOrDefault(p => p?.Name == serviceName);
    }

    private static (Type tIn, Type tOut) GetInputOutputTypes(Type containerType)
    {
        var iProcessorInterface = containerType.FindInterfaces(InterfaceFilter, typeof(IProcessor<,>).Name).First();
        var genericArguments = iProcessorInterface.GetGenericArguments();
        var tIn = genericArguments.First();
        var tOut = genericArguments.Last();
        return (tIn, tOut);
    }

    private static bool InterfaceFilter(Type typeObj, object? criteriaObj)
    {
        var typeName = typeObj.ToString();
        var criteriaOrEmpty = criteriaObj?.ToString() ?? string.Empty;
        return typeName.Contains(criteriaOrEmpty);
    }

    public TOut? Process<TIn, TOut>(string serviceName, TIn input, CancellationToken token)
    {
        var processor = (IProcessor<TIn, TOut>)GetProcessor(serviceName)!;
        if (processor == null)
        {
            throw new UnknownProcessorException(serviceName);
        }

        var processorType = processor.GetType();
        var (tIn, tOut) = GetInputOutputTypes(processorType);

        var method = processorType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(mi => mi.ReturnType == tOut && mi.GetParameters().Any(p => p.ParameterType == tIn) &&
                         mi.Name == "Process");
        return (TOut)method.Invoke(processor, new[] { (object)input!, token })!;
    }

    public object? Process(string processorName, string? message, CancellationToken token)
    {
        var processor = GetProcessor(processorName);
        if (processor == null)
        {
            throw new UnknownProcessorException(processorName);
        }
        var processorType = processor.GetType();
        var (tIn, tOut) = GetInputOutputTypes(processorType);
        var processorOutput = typeof(ProcessorOutput<>);
        tOut = processorOutput.MakeGenericType(tOut);
        var input = message == null ? null : JsonConvert.DeserializeObject(message, tIn);
        var method = processorType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(mi => mi.ReturnType == tOut && mi.GetParameters().Any(p => p.ParameterType == tIn) &&
                         mi.Name == "Process");
        var result =  method.Invoke(processor, new[] { input, token });
        return result;
    }

    public IEnumerator<IProcessor> GetEnumerator()
    {
        return Processors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Processors).GetEnumerator();
    }
}