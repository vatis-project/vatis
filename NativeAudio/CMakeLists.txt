cmake_minimum_required(VERSION 3.5.1)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

project(NativeAudio)

if(APPLE)
    # Disable runtime linking to prevent issues with backends that are not actually implemented
    add_definitions(-DMA_NO_RUNTIME_LINKING)

    # Set architectures to build fat library
    set(CMAKE_OSX_ARCHITECTURES "arm64;x86_64")
endif()

add_definitions(-DMA_NO_JACK)
add_definitions(-DMA_NO_WINMM)
add_definitions(-DMA_NO_NULL)
add_definitions(-DMA_NO_CUSTOM)

add_library(${PROJECT_NAME} SHARED)

# Specify include directory
target_include_directories(${PROJECT_NAME} PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/include)

# Add source files
target_sources(${PROJECT_NAME}
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}/src/native_audio.cpp
        ${CMAKE_CURRENT_SOURCE_DIR}/src/audio_context.cpp
        ${CMAKE_CURRENT_SOURCE_DIR}/src/wav_data.cpp
        ${CMAKE_CURRENT_SOURCE_DIR}/src/miniaudio.c
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include/native_audio.h
        ${CMAKE_CURRENT_SOURCE_DIR}/include/audio_context.h
        ${CMAKE_CURRENT_SOURCE_DIR}/include/miniaudio.h
        ${CMAKE_CURRENT_SOURCE_DIR}/include/sound_type.h
        ${CMAKE_CURRENT_SOURCE_DIR}/include/wav_data.h
)

if(APPLE)
    find_library(CORE_AUDIO CoreAudio)
    find_library(AUDIO_TOOLBOX AudioToolbox)
    find_library(AUDIO_UNIT AudioUnit)
    find_library(CORE_FOUNDATION CoreFoundation)
    find_library(CORE_SERVICES CoreServices)

    target_link_libraries(${PROJECT_NAME}
        PRIVATE
        ${CORE_AUDIO}
        ${AUDIO_TOOLBOX}
        ${AUDIO_UNIT}
        ${CORE_FOUNDATION}
        ${CORE_SERVICES})

    # Copy the built dylib to the Native folder, creating a subfolder for macOS
    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E make_directory
            "${CMAKE_CURRENT_BINARY_DIR}/../../vATIS.Desktop/Voice/Audio/Native/macos"
        COMMAND ${CMAKE_COMMAND} -E copy
            "$<TARGET_FILE:${PROJECT_NAME}>"
            "${CMAKE_CURRENT_BINARY_DIR}/../../vATIS.Desktop/Voice/Audio/Native/macos/$<TARGET_FILE_NAME:${PROJECT_NAME}>"
    )
endif()

if(WIN32)
    # Set multi-threaded runtime for Debug and Release
    target_compile_options(${PROJECT_NAME} PRIVATE 
        $<$<CONFIG:Debug>:/MTd> 
        $<$<CONFIG:Release>:/MT>
    )

    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E make_directory
            "${CMAKE_CURRENT_BINARY_DIR}/../../vATIS.Desktop/Voice/Audio/Native/win"
        COMMAND ${CMAKE_COMMAND} -E copy
            "$<TARGET_FILE:${PROJECT_NAME}>"
            "${CMAKE_CURRENT_BINARY_DIR}/../../vATIS.Desktop/Voice/Audio/Native/win/$<TARGET_FILE_NAME:${PROJECT_NAME}>"
    )
endif()

if(UNIX AND NOT APPLE)
    target_link_libraries(${PROJECT_NAME} -lpthread -lm -ldl)

    target_compile_options(${PROJECT_NAME} PRIVATE 
        $<$<CONFIG:Debug>:-g> # Enable debug symbols for Debug builds
        $<$<CONFIG:Release>:-O2> # Optimize for Release builds
    )
    
    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E make_directory
            "${CMAKE_CURRENT_BINARY_DIR}/../../vATIS.Desktop/Voice/Audio/Native/lin"
        COMMAND ${CMAKE_COMMAND} -E copy
            "$<TARGET_FILE:${PROJECT_NAME}>"
            "${CMAKE_CURRENT_BINARY_DIR}/../../vATIS.Desktop/Voice/Audio/Native/lin/$<TARGET_FILE_NAME:${PROJECT_NAME}>"
    )
endif()