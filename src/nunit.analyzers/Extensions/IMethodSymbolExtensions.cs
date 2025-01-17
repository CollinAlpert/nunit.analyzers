using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Analyzers.Constants;

namespace NUnit.Analyzers.Extensions
{
    internal static class IMethodSymbolExtensions
    {
        /// <summary>
        /// Gets the parameters into required, optional, and params counts.
        /// </summary>
        /// <param name="this">The <see cref="IMethodSymbol"/> reference to get parameters from.</param>
        /// <returns>
        /// The first count is the required parameters, the second is the optional count,
        /// and the last is the <see langword="params" /> count.
        /// </returns>
        internal static (uint requiredParameters, uint optionalParameters, uint paramsCount) GetParameterCounts(
            this IMethodSymbol @this)
        {
            var parameters = @this.Parameters;

            var requiredParameters = default(uint);
            var optionalParameters = default(uint);
            var paramsParameters = default(uint);

            foreach (var parameter in parameters)
            {
                if (parameter.IsOptional)
                {
                    optionalParameters++;
                }
                else if (parameter.IsParams)
                {
                    paramsParameters++;
                }
                else
                {
                    requiredParameters++;
                }
            }

            return (requiredParameters, optionalParameters, paramsParameters);
        }

        /// <summary>
        /// Returns true if method is implementation of method in interface.
        /// </summary>
        internal static bool IsInterfaceImplementation(this IMethodSymbol @this, string interfaceFullName)
        {
            var interfaceType = @this.ContainingType.AllInterfaces.FirstOrDefault(i => i.GetFullMetadataName() == interfaceFullName);

            if (interfaceType is null)
                return false;

            return interfaceType.GetMembers().OfType<IMethodSymbol>()
                .Any(interfaceMethod => interfaceMethod.Name == @this.Name
                    && SymbolEqualityComparer.Default.Equals(@this.ContainingType.FindImplementationForInterfaceMember(interfaceMethod), @this));
        }

        internal static bool IsTestRelatedMethod(this IMethodSymbol methodSymbol, Compilation compilation)
        {
            return methodSymbol.HasTestRelatedAttributes(compilation) ||
                (methodSymbol.OverriddenMethod is not null && methodSymbol.OverriddenMethod.IsTestRelatedMethod(compilation));
        }

        internal static bool HasTestRelatedAttributes(this IMethodSymbol methodSymbol, Compilation compilation)
        {
            return methodSymbol.GetAttributes().Any(
                a => a.IsTestMethodAttribute(compilation) || a.IsSetUpOrTearDownMethodAttribute(compilation));
        }

        internal static bool IsTestFixture(this ITypeSymbol typeSymbol, Compilation compilation)
        {
            return typeSymbol.GetMembers().OfType<IMethodSymbol>().Any(m => m.IsTestRelatedMethod(compilation));
        }

        internal static bool IsInstancePerTestCaseFixture(this ITypeSymbol typeSymbol, Compilation compilation)
        {
            // Is there a FixtureLifeCycleAttribute?
            AttributeData? fixtureLifeCycleAttribute = typeSymbol.GetAllAttributes().FirstOrDefault(x => x.IsFixtureLifeCycleAttribute(compilation));
            return fixtureLifeCycleAttribute is not null &&
                fixtureLifeCycleAttribute.ConstructorArguments.Length == 1 &&
                fixtureLifeCycleAttribute.ConstructorArguments[0] is TypedConstant typeConstant &&
                typeConstant.Kind == TypedConstantKind.Enum &&
                typeConstant.Type.IsType(NUnitFrameworkConstants.FullNameOfLifeCycle, compilation) &&
                typeConstant.Value is 1 /* LifeCycle.InstancePerTestCase */;
        }
    }
}
