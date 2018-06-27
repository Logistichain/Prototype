# Testing

## Unit testing
Because this is still a prototype, only the most critical parts of the code are covered by unit tests: Consensus validators. The [PowBlockValidator.cs](../src/Logistichain.Consensus.BlockLogic.PowBlockValidator.cs) and [StateTransactionValidator.cs](../src/Logistichain.Consensus.TransactionLogic.StateTransactionValidator.cs) classes must be fully covered correctly.
Other components which aren't subject to regular change are also good to test.

## Functional testing
A test documentat was created to test functionalities and to get you familiar with the software: [functional tests](docs/functionaltests.pdf). 