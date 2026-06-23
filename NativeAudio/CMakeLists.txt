cmake_minimum_required(VERSION 3.16)

project(NativeAudio LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

set(NATIVE_DLL_OUTPUT_DIR "${CMAKE_SOURCE_DIR}/../vATIS.Desktop/Voice/Audio/Native" CACHE PATH "Path to copy built library")

if(APPLE)
    # Disable runtime linking to prevent issues with backends that are not actually implemented.
    add_definitions(-DMA_NO_RUNTIME_LINKING)

    if(NOT DEFINED CMAKE_OSX_DEPLOYMENT_TARGET)
        set(CMAKE_OSX_DEPLOYMENT_TARGET "12.0" CACHE STRING "Minimum macOS Version" FORCE)
    endif()
endif()

add_definitions(-DMA_NO_JACK)
add_definitions(-DMA_NO_WINMM)
add_definitions(-DMA_NO_NULL)
add_definitions(-DMA_NO_CUSTOM)

add_library(${PROJECT_NAME} SHARED)

if(WIN32)
    set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")
endif()

target_include_directories(${PROJECT_NAME} PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/include)

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

    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E make_directory
            "${NATIVE_DLL_OUTPUT_DIR}/macos"
        COMMAND ${CMAKE_COMMAND} -E copy
            "$<TARGET_FILE:${PROJECT_NAME}>"
            "${NATIVE_DLL_OUTPUT_DIR}/macos/$<TARGET_FILE_NAME:${PROJECT_NAME}>"
    )
endif()

if(WIN32)
    if(MSVC)
        # Keep MSVC builds on the static CRT, while allowing MinGW/Zig cross-builds from Linux.
        target_compile_options(${PROJECT_NAME} PRIVATE
            $<$<CONFIG:Debug>:/MTd>
            $<$<CONFIG:Release>:/MT>
        )
    endif()

    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E make_directory
            "${NATIVE_DLL_OUTPUT_DIR}/win"
        COMMAND ${CMAKE_COMMAND} -E copy
            "$<TARGET_FILE:${PROJECT_NAME}>"
            "${NATIVE_DLL_OUTPUT_DIR}/win/$<TARGET_FILE_NAME:${PROJECT_NAME}>"
    )
endif()

if(UNIX AND NOT APPLE)
    target_link_libraries(${PROJECT_NAME} PRIVATE pthread m dl)

    target_compile_options(${PROJECT_NAME} PRIVATE
        $<$<CONFIG:Debug>:-g>
        $<$<CONFIG:Release>:-O2>
    )

    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E make_directory
            "${NATIVE_DLL_OUTPUT_DIR}/lin"
        COMMAND ${CMAKE_COMMAND} -E copy
            "$<TARGET_FILE:${PROJECT_NAME}>"
            "${NATIVE_DLL_OUTPUT_DIR}/lin/$<TARGET_FILE_NAME:${PROJECT_NAME}>"
    )
endif()
