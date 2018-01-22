﻿using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;

namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyTypeGenerator : IProxyTypeGenerator
    {
        private readonly ModuleBuilder _moduleBuilder;
        private IGenerateTypeOperator _defineTypeOperator;
        private IGenerateTypeOperator _defineFieldsOperator;
        private IGenerateTypeOperator _implementConstructorsOperator;
        private IGenerateTypeOperator _implementMethodsOperator;

        private IGenerateTypeOperator DefineTypeOperator
        {
            get
            {
                if(_defineTypeOperator==null)                                             {
                    _defineTypeOperator = new DefineTypeOperator();
                }

                return _defineTypeOperator;
            }
        }

        private IGenerateTypeOperator DefineFieldsOperator
        {
            get
            {
                if (_defineFieldsOperator == null)
                {
                    _defineFieldsOperator = new DefineFieldsOperator();
                }

                return _defineFieldsOperator;
            }
        }

        private IGenerateTypeOperator ImplementConstructorsOperator
        {
            get
            {
                if(_implementConstructorsOperator == null)
                {
                    _implementConstructorsOperator = new ImplementConstructorsOperator();
                }

                return _implementConstructorsOperator;
            }
        }

        private IGenerateTypeOperator ImplementMethodsOperator
        {
            get
            {
                if(_implementMethodsOperator == null)
                {
                    _implementMethodsOperator = new ImplementMethodOperator();
                }

                return _implementMethodsOperator;
            }
        }

        public ProxyTypeGenerator()
        {
            _moduleBuilder = ProxyConfiguration.Configuration.ProxyModuleBuilder;
            _defineTypeOperator = new DefineTypeOperator();
        }

        public ProxyDescriptor GenerateProxyByClass(Type classType)
        {
            if (classType == null)
            {
                throw new ArgumentNullException(nameof(classType));
            }

            return GenerateProxyType(null, classType);
        }

        public ProxyDescriptor GenerateProxyByInterface(Type interfaceType, Type classType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            if (classType == null)
            {
                throw new ArgumentNullException(nameof(classType));
            }

            return GenerateProxyType(interfaceType, classType);
        }

        private ProxyDescriptor GenerateProxyType(Type interfaceType, Type classType)
        {
            var context = new GeneratorTypeContext
            {
                ModuleBuilder = _moduleBuilder,
                ClassType = classType,
                InterfaceType = interfaceType
            };

            DefineTypeOperator.Generate(context);
            DefineFieldsOperator.Generate(context);

            ImplementConstructorsOperator.Generate(context);
            ImplementMethodsOperator.Generate(context);

            var proxyType = context.TypeBuilder.CreateTypeInfo();
            HandleCollection.AddHandles(proxyType.MetadataToken, context.MethodHandles);
            return new ProxyDescriptor
            {
                InterfaceType = interfaceType,
                ClassType = classType,
                ProxyType = proxyType,
            };
        }

        private static string GetTypeName(Type classType, int token)
        {
            return $"{classType.Name}_AspectFlare_DynamicProxy_{token}";
        }
    }
}
