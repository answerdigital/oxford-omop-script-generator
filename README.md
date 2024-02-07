# Oxford OMOP script generator

This tool interrogates clinical data and attempts to detect the order in which the data files should be loaded and transformed.

It supports the following data types
* CDS
* COSD
* SACT
* RTDS

It generates two powershell script files, which can be used to undertake the ETL process.
* `stage-and-transform.ps1` is used to stage then transform the data.
* `stage-only.ps1` can be used to just only stage the data. This option leaves the data in the staging tables.

The tool can be configured by the `appsetting.json` file.

| Setting             | Explanation                            |
|---------------------|----------------------------------------|
| PathToCdsDirectory  | Directory of CDS data                  |
| PathToCosdDirectory | Directory of COSD data                 |
| PathToSactDirectory | Root directory of SACT data            |
| PathToRtdsDirectory | Root directory of RTDS data            |
| OmopToolPath        | Path to OMOP importing tool to call    |
| OutputPath          | Output directory for generated scripts |

```
{
    "PathToCdsDirectory": "D:\\OMOP_Mapping\\EMIS\\CDS_v6.2",
    "PathToCosdDirectory": "D:\\Cancer_Reporting\\COSD",
    "PathToSactDirectory": "D:\\Cancer_Reporting\\SACT\\SACT V.3",
    "PathToRtdsDirectory": "D:\\Cancer_Reporting\\RTDS",
    "OmopToolPath": "publish\\publish\\omop.exe",
    "OutputPath": "../../"
}
```

# Usage

```
.\oxford-import-script-generator.exe
```