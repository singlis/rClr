rClr
====
# Scott's Notes for building this package on VS2017
## Prerequisites
 * VS2017
 * RTools https://cran.r-project.org/bin/windows/Rtools/
 * MikTeX (full package) https://miktex.org/download
 * R
 * install.packages(c('testthat','R6','plyr','formatR','xtable'))

## Building
 * Open a VS2017 Developer Command Prompt (Very important!! Regular cmd prompt does not work)
 * cd to the parent directory of the rClr enlistment
 * Type the following:
```
R CMD check .\rClr
```
If everything passes, you can now build the binary package by typing:
```
R CMD INSTALL .\rClr
R CMD INSTALL --build .\rClr
```
This will generate a rClr<version>.zip file.

## Errors
It is more than likely you will hit an error due to a missing configiration or dependency that I have missed. Please look at the install.out file as this will give a lot of good information on what the cause of the error is.


Accessing the Common Language Runtime (.NET or Mono) from the R statistical software, in-process.

# Installing

As of September 2015, the source code snapshot of tagged releases can be found via the [release tab of the rClr GitHub repository](https://github.com/jmp75/rClr/releases). Binary R packages for Windows cannot be released via github, and you can get them from [https://rclr.codeplex.com](https://rclr.codeplex.com).

## Pre-compiled binaries

You can install pre-compiled rClr for Windows via [https://rclr.codeplex.com](https://rclr.codeplex.com). You can use from the command line `R CMD INSTALL rclr_0.8.zip` where `R` points to one of the R.exe installed on your machine, or from R itself `install.packages('c:/path/to/rclr_0.8.zip')`

## From source

Installing on Linux is always installing from source anyway, be it from a tarball, cloning the repo, or using `devtools`. 

rClr is not your average R package and requires a few more tools than is typical for most R packages. 

On Windows you will need a C# and C and/or Visual C++ compiler. Using the Visual Studio 2013 toolchain is recommended. Read [the current build instructions](https://r2clr.codeplex.com/wikipage?title=Build%20instructions&referringTitle=Documentation). Note that as of September 2015 using Mono on Windows is not maintained.

A Linux distribution with R, g++ and the Mono toolchain (including xbuild) should work. Note that while a range of Mono versions in the 3.X series may work, I recommend you use versions 3.8 or above. This may require you to look for adequate versions (for instance Debian is lagging behind currently). You may want to have a look at the instructions at the [mono download page for Linux](http://www.mono-project.com/download/#download-lin) and use the Xamarin packages.

You should be able to install the package using the `install_github` function of the package `devtools`. The following commands have been tested successfully on Windows with VS2013 and Linux with Mono 3.10, on 2014-12-19. 

```R
# Optionally you may remove a prior package
remove.packages('rClr')
library(devtools)
install_github("jmp75/rClr", build_vignettes=TRUE)
```

NOTE: you must have a fully working devtools package. If devtools, on loading, reports a warning about not finding a suitable version of RTools (on Windows), this may prevent it from installing rClr. The issue has been seen for instance using devtools 1.7.0, installed from CRAN, via R 3.2.2. Package devtools 1.7.0 seems to require RTools 3.1, even when run from R 3.2.2. One way to overcome this is to install devtools from a more recent download, from its github repository.

# Getting started

The package contains documentation, code sample and a vignette to get started.

```S
library(rClr)
?rClr
# There is an HTML vignette:
browseVignettes('rClr')
```

You will otherwise find some documentation at [https://r2clr.codeplex.com/documentation](https://r2clr.codeplex.com/documentation)

# Feedback and contributions

While this package is sometimes used for the author's paid day job, this is largely a personal endeavour. Support is appreciated in many forms.

* Citations: As of December 2014, [A presentation given at the R user conference 2013](https://publications.csiro.au/rpr/pub?list=ASE&pid=csiro:EP132284&expert=false&sb=RECENT&n=6&rpp=50&page=17&tr=3274&dr=all&csiro.affiliation=B3800). A journal paper will, hmm, "soon" follow.
* Documentation: reporting issues, feature requests or discussion threads as such can be very valuable material if done well.  
* Consulting or contract work is an option that may be arranged.

# Related work

A few packages using rClr are publicly accessible, and may be of interest if you want to build your own package with dependencies on rClr.

* If you are interested in environmental modelling: [RtoTIME](https://github.com/jmp75/RtoTIME) is a package that depends on rClr
* [rsqlserver](https://github.com/agstudy/rsqlserver) is an Sql Server driver database interface (DBI) driver for R
* [A package to access optimization tools on .NET](https://github.com/jmp75/metaheuristics/tree/master/R/pkgs/mh)
