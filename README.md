XL - Excel-like tool in .NET & SQL Server manner.

Features:
1. Basic operations (+, -, *, /)
2. Text, Number and Formula data types
3. Formulas can reference other cells
4. Circular reference detection
5. Whitespaces inside formulas

Ideas:
* This project could be further improved with introduction of functions: SUM, AVG etc.


### Run

`docker-compose up` in the root folder. 

### Try

After launch - go to <a href="http://localhost:8080/swagger">Swagger endpoint</a> or execute your own tests against the API methods.

### Test

Execute command in root folder to run tests in separate Docker container

Powershell:

`docker build -t xl.tests -f XL.Tests/Dockerfile .; docker run xl.tests`

CMD:

`docker build -t xl.tests -f XL.Tests/Dockerfile . && docker run xl.tests`
