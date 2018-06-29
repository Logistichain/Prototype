# Logistichain architecture
As part of the 4+1 architecture design, several views are available to give you a complete view of the system's architecture:
- Scenario's in the form of a [use case diagram](Architecture/Use-case-diagram.png): All required functionalities
- Logical view as a [class diagram](Architecture/Class-diagram.png): Give substance to the functionalities
- Development view with a [component diagram](Architecture/Development.png): Describe system artifacts
- Process view in the form of several activity diagrams:
	- [Block validation](BlockValidation.md)
	- [Transaction validation](TxValidation.md)
	- Establishing a connection with another node using a [handshake procedure](Architecture/Connection-setup-process.png)
	- Synchronizing with another node using the [Initial Block Download procedure](Architecture/IBD-process.png)
- Physical view as a [deployment diagram](Architecture/Deployment.png): Show the communications between the software assemblies as well as the physical machines