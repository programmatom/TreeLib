Welcome to the TreeLib repository
===

TreeLib provides balanced binary tree implementations with optional rank augmentation for supporting statistical queries (e.g. median or percentile) or sparse mappings. TreeLib is written in C# and targets the .NET Framework.

Using
===

The documentation for TreeLib is located at http://programmatom.github.io/TreeLib

Contributing
===
Contributions are welcome. Please contact me if you find a bug, have a feature request, or want to create a pull request.

Please do keep in mind that I want to keep the basic tree implementations as streamlined as possible. On the other hand, I am interested in exposing operations that can be done more efficiently at the level of tree nodes, rather than the collection-oriented interfaces that are exposed to consumers of the library.

There are no formal coding guidelines at this time. Just make new code look like what's already there.

Building
===
The TreeLib solution builds with Visual Studio 2015 Community Edition. Open the solution file located at TreeLib\Treelib.sln. There are several projects contained therein:

Project|Description
---|---
BuildTool|A set of Roslyn transformations that are used to generate specialized implementations of various trees (see below)
Template|A non-building project containing the master (base) implementations of each category of collection
TreeLibInterface|Builds the assembly containing the interfaces implemented by various tree specializations
TreeLib|Builds the assembly containing "standard" specializations of trees
TreeLibArray|Builds the assembly containing array-based specialization of trees
TreeLibLong|Builds the assembly containing specialization of trees using `long` rank values instead of `int`
TreeLibUtil|Builds the assembly containing utility classes that use the trees
TreeLibTest|Builds the unit and stochastic test program

BuildTool
===
A core concept of the building of TreeLib is that there is only one master implementation of each category of class (AVL, Red-Black, Splay, and HugeList) which is processed to derive specializations which have been stripped of all unneeded internals. This is analogous to what one might do in C++ using selector-parameterized templates, relying on the optimizing compiler to eliminate unneeded functionality through constant propagation and dead code elimination. However, the .NET Framework's implementation of generics is much more restrictive and does not provide a workable means of achieving such specialization. Instead, a set of Roslyn transforms are used to specialize the code and eliminate unneeded portions of the implementation.

Currently, `BuildTool` is still in an experimental state. It gets the job done, but it is quite slow, fairly specific to the task at hand (with a number of hard-coded transformations), and not well integrated into the build. If a change is made to a master implementation file, one must manually run "Rebuild All" since MSBuild does not know about the dependencies around `BuildTool`.

Making Changes
===
The first step in adding functionality or making a change is to edit the Interfaces.cs file in the TreeLibInterface project to add relevant public interfaces that define the functionality to be added (or editing existing interfaces - but beware of breaking compatibility).

Then, modify or add to the master/base implementations in the Template project. Adding a new master/base implementation is fairly involved, since you'll need to wrestle with `BuildTool` and possibly extend it. Making minor changes to existing code is less hairy, but may still involve a confrontation with `BuildTool`.

If new specializations have been added, be sure to add the products of BuildTool in each target project's Generated subdirectory to the VS project in order to get them built.

Most important, add testing for new or changed functionality.

Testing
===
Testing is the most important (and most tedious) aspect of maintaining a library like TreeLib. The `TreeLibTest.exe` provides comprehensive unit, stochastic, and performance testing. There are three important categories of testing. The test tool has several arguments which can all be combined and are interpreted left-to-right, although not all make sense together.

Argument|Description
---|---
`-unit`|Disable the unit test phase
`+unit`|Enable the unit test phase. Individual tests specifically disabled will remain disabled
`-unit:<name>`|Disable a particular unit test. `all` may be specified for `<name>` to disable all tests. Use `all` followed by `+unit:<name>` to enable just one test
`+unit:<name>`|Enable a particular unit test
---|---
`-memory`|Disable the memory allocation regression tests
`+memory`|Enable the memory allocation regression tests
---|---
`-random`|Disable the stochastic test phase
`+random`|Enable the stochastic test phase. Individual tests specifically disabled will remain disabled
`-random:<name>`|Disable a particular stochastic test. `all` may be specified for `<name>` to disable all tests. Use `all` followed by `+random:<name>` to enable just one test
`+random:<name>`|Enable a particular stochastic test
---|---
`-perf`|Disable performance testing (perf testing is enabled by default in the `Release` build and disabled in the `Debug` build)
`+perf`|Enable performance testing
`baseline`|Cause a new baseline measurement to be made. If omitted, the current run will be compared to any existing baseline and deviations (faster or slower) will be written to the output
---|---
`break:<iteration>`|Generate an assert and `Debugger.Break()` at the specified iteration. This is useful for rerunning and breaking immediately before a test failure.
`seed:<number>`|Specify the seed to use for the random number generator in stochastic tests. This allows failures to be reproduced upon rerun, and debugged if used in conjunction with `break`.

Unit Tests
---
Each new category of specialization should be covered by a new unit test module (represented by one source file in TreeLibTest\UnitTests). Modifications to existing implementations should have tests added in existing modules.

Some testing is done by explicitly checking for expected results when invoking exposed methods. A lot of testing is done by comparison of outputs with very simple "reference" implementations that can be seen to be correct to a high degree of confidence.

Stochastic Tests
---
There are a set of randomized stress tests that can pound on the generated implementations indefinitely (overnight is good). The randomized tests should make an effort to test boundary cases of all exposed methods, including invalid inputs. All stochastic tests include the appropriate "reference" implementation in the battery and ensure that all implementations produce the same results.

Memory Allocation Regression Tests
---
The test suite includes memory allocation regression tests to detect if code changes have increased
(or decreased) the number or size of memory allocations. The tests require [CLR Profiler][2] ([download:][3] [CLRProfiler45Binaries][4]) to be installed as `C:\Program Files\CLRProfiler45Binaries\{bits}\CLRProfiler.exe` where `{bits}` is `32` or `64` based on the target settings for `TreeLibTest.exe`. (This can be overridden with the `CLR_PROFILER_PATH_32` and `CLR_PROFILER_PATH_64` environment variables, which specify an alternate full path to the appropriate `CLRProfiler.exe`.)

Performance Tests
---
If major changes are being made to something, a performance test should be run to ensure regressions do not happen. Performance tests are run only on the `Release` build.

The first step of running a performance test is to create a baseline on the unchanged project:

`TreeLibTest.exe -unit -random +perf baseline`

After changes are made, rerun the performance test:

`TreeLibTest.exe -unit -random +perf`

The tolerances are fairly strict at this time, so there may be spurious failures reported. However, deviations should be investigated before being dismissed.

Also, I have run into problems on CPUs with thermal management, especially AMD's Turbo Core but also Intel's Turbo-Boost. I'd recommend disabling this feature during performance test runs. There can also be an issue with ambient temperature, where the warmer the weather, the slower the processor runs under load. This is particularly an issue if testing on a laptop. Try to run the baseline and the actual test at similar ambient temperatures.

Code Coverage
===
The project is currently around 97% covered by the unit and stochastic tests. The goal is 100% coverage.

License
===
TreeLib is licensed under the [GNU Lesser General Public License][1].

Thank You
===
Thanks for your interest in TreeLib!

[1]: http://www.gnu.org/licenses/lgpl-3.0-standalone.html
[2]: http://clrprofiler.codeplex.com/
[3]: http://clrprofiler.codeplex.com/releases/view/97738
[4]: http://clrprofiler.codeplex.com/downloads/get/532810

