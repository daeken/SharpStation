// ReSharper disable RedundantUsingDirective
// ReSharper disable SwitchStatementMissingSomeCases
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable ArrangeRedundantParentheses
// ReSharper disable RedundantCast
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
#pragma warning disable 162
using System;

namespace SharpStation {
	public partial class BaseCpu {
		internal string Disassemble(uint pc, uint inst) {
			/*<<GENERATED>>*/
			return $"Unknown instruction at {pc:X8}: {inst:X8}";
		}
	}
}