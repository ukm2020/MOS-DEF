cmake_minimum_required(VERSION 3.20)
project(mos-def C)

# Set C standard
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Force x64 architecture
if(NOT CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(FATAL_ERROR "MOS-DEF must be built for 64-bit architecture")
endif()

# Set output directory
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/artifacts)

# Create executable
add_executable(mos-def
    src/cli.c
    src/enum.c
    src/rotate.c
    src/config.c
    src/util.c
)

# Link required libraries
target_link_libraries(mos-def user32 gdi32)

# Include directories
target_include_directories(mos-def PRIVATE src)

# Compiler flags for production build
if(MSVC)
    target_compile_options(mos-def PRIVATE /W4 /WX)
    target_compile_definitions(mos-def PRIVATE _CRT_SECURE_NO_WARNINGS)
else()
    target_compile_options(mos-def PRIVATE -Wall -Wextra -Werror)
endif()

# Strip debug info for release builds
if(CMAKE_BUILD_TYPE STREQUAL "Release")
    if(MSVC)
        target_link_options(mos-def PRIVATE /DEBUG:NONE)
    else()
        target_link_options(mos-def PRIVATE -s)
    endif()
endif()
