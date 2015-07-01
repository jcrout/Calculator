# Calculator
Current project. Graphing Calculator. C#

The goal of this project is to create a robust, highly-extensible graphing calculator. The program should support extension DLLs that can be inserted/removed without breaking the application. Most of the core types should either implement abstract base classes or interfaces with included default implementations, and types that consume these should operate on the contracts specified by these base classes and interfaces.

This is a project that I am working on currently, and it is still far from finished. The GUI is still primitive at this stage. Parsing text into equation delegates and graphing them is doable at this time, but more work is still needed in these areas too.

Current classes implementing base abstract base classes:
Equation
EquationParser
EquationBox
EquationBoxContainer
ConstantBox
CloseBox

Many more to come in the near future.
