# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 

# Getting Started
TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:
1.	Installation process
2.	Software dependencies
3.	Latest releases
4.	API references

# Build and Test
TODO: Describe and show how to build your code and run the tests. 

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://www.visualstudio.com/en-us/docs/git/create-a-readme). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)


Adicionar propiedad para ignorar guardado de datos con incrementales - tabla ordenes

entity.Property(e => e.AccountingRelationship).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);


Scaffold-DbContext "Server=(localdb)\LocalDBA;Database=cyberforgepc;User Id=DONOVAN\donov;Integrated Security=SSPI" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Database\Models -force -ContextDir Database\Context -context CyberforgepcContext